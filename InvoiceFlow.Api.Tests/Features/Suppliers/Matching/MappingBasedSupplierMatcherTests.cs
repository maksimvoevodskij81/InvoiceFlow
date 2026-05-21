using InvoiceFlow.Api.Features.Invoices;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Features.Suppliers.Idempotency;
using InvoiceFlow.Api.Features.Suppliers.Matching;

namespace InvoiceFlow.Api.Tests.Features.Suppliers.Matching;

public sealed class MappingBasedSupplierMatcherTests
{
    private static MappingBasedSupplierMatcher BuildMatcher(
        ISupplierMappingStore? supplierStore = null,
        IBankAccountMappingStore? bankStore = null)
    {
        return new MappingBasedSupplierMatcher(
            supplierStore ?? new FakeSupplierMappingStore(),
            bankStore ?? new FakeBankAccountMappingStore(),
            new SupplierFingerprintBuilder(),
            new BankAccountFingerprintBuilder());
    }

    [Fact]
    public async Task MatchAsync_ShouldReturnMatchedWithReview_WhenIbanMappingFound()
    {
        var bankStore = new FakeBankAccountMappingStore();
        bankStore.Seed("IBAN:NL91ABNA0417164300", "exact-001");

        var result = await BuildMatcher(bankStore: bankStore).MatchAsync(
            new InvoiceParseResult
            {
                SupplierName = "Acme B.V.",
                SupplierBankAccount = "NL91 ABNA 0417 1643 00"
            },
            CancellationToken.None);

        Assert.True(result.IsMatched);
        Assert.True(result.RequiresReview);
        Assert.Equal("exact-001", result.ExactSupplierId);
        Assert.Equal(SupplierMatchSources.BankAccount, result.MatchedBy);
    }

    [Fact]
    public async Task MatchAsync_ShouldReturnMatchedWithReview_WhenNamePostcodeMappingFound()
    {
        var invoice = new InvoiceParseResult { SupplierName = "Acme B.V.", SupplierPostcode = "1234 AB" };
        string fingerprint = new SupplierFingerprintBuilder().BuildNamePostcode(invoice);

        var supplierStore = new FakeSupplierMappingStore();
        supplierStore.Seed(fingerprint, "exact-002");

        var result = await BuildMatcher(supplierStore: supplierStore).MatchAsync(invoice, CancellationToken.None);

        Assert.True(result.IsMatched);
        Assert.True(result.RequiresReview);
        Assert.Equal("exact-002", result.ExactSupplierId);
        Assert.Equal(SupplierMatchSources.Name, result.MatchedBy);
    }

    [Fact]
    public async Task MatchAsync_ShouldReturnMatchedWithReview_WhenNameAddressPostcodeMappingFound()
    {
        var invoice = new InvoiceParseResult
        {
            SupplierName = "Acme B.V.",
            SupplierAddressLine = "Keizersgracht 1",
            SupplierPostcode = "1234 AB"
        };
        string fingerprint = new SupplierFingerprintBuilder().BuildNameAddressPostcode(invoice);

        var supplierStore = new FakeSupplierMappingStore();
        supplierStore.Seed(fingerprint, "exact-003");

        var result = await BuildMatcher(supplierStore: supplierStore).MatchAsync(invoice, CancellationToken.None);

        Assert.True(result.IsMatched);
        Assert.True(result.RequiresReview);
        Assert.Equal("exact-003", result.ExactSupplierId);
        Assert.Equal(SupplierMatchSources.Name, result.MatchedBy);
    }

    [Fact]
    public async Task MatchAsync_ShouldReturnNotMatched_WhenNoMappingFound()
    {
        var result = await BuildMatcher().MatchAsync(
            new InvoiceParseResult
            {
                SupplierName = "Unknown Supplier",
                SupplierPostcode = "9999 ZZ",
                SupplierBankAccount = "NL99XXXX0000000000"
            },
            CancellationToken.None);

        Assert.False(result.IsMatched);
    }

    [Fact]
    public async Task MatchAsync_ShouldReturnMatchedWithReview_WhenNonIbanBankAccountMappingFound()
    {
        var bankStore = new FakeBankAccountMappingStore();
        bankStore.Seed("BANKACCOUNT:50200012345678", "exact-004");

        var result = await BuildMatcher(bankStore: bankStore).MatchAsync(
            new InvoiceParseResult
            {
                SupplierName = "Tata Consultancy Services",
                SupplierBankAccount = "502 000 123 456 78"
            },
            CancellationToken.None);

        Assert.True(result.IsMatched);
        Assert.True(result.RequiresReview);
        Assert.Equal("exact-004", result.ExactSupplierId);
        Assert.Equal(SupplierMatchSources.BankAccount, result.MatchedBy);
    }

    [Fact]
    public async Task MatchAsync_ShouldReturnNotMatched_WhenSupplierFieldsAreMissing()
    {
        var result = await BuildMatcher().MatchAsync(
            new InvoiceParseResult
            {
                SupplierName = string.Empty,
                SupplierPostcode = null,
                SupplierBankAccount = null,
                SupplierAddressLine = null
            },
            CancellationToken.None);

        Assert.False(result.IsMatched);
    }

    [Fact]
    public async Task MatchAsync_ShouldReturnMatchedWithReview_WhenKvkMappingFound()
    {
        string fingerprint = new SupplierFingerprintBuilder().BuildKvK("12345678");
        var supplierStore = new FakeSupplierMappingStore();
        supplierStore.Seed(fingerprint, "exact-kvk-01");

        var result = await BuildMatcher(supplierStore: supplierStore).MatchAsync(
            new InvoiceParseResult
            {
                SupplierName      = "Acme B.V.",
                SupplierKvKNumber = "12345678"
            },
            CancellationToken.None);

        Assert.True(result.IsMatched);
        Assert.True(result.RequiresReview);
        Assert.Equal("exact-kvk-01", result.ExactSupplierId);
        Assert.Equal(SupplierMatchSources.KvK, result.MatchedBy);
    }

    [Fact]
    public async Task MatchAsync_ShouldReturnMatchedWithReview_WhenVatMappingFound()
    {
        string fingerprint = new SupplierFingerprintBuilder().BuildVat("NL123456789B01");
        var supplierStore = new FakeSupplierMappingStore();
        supplierStore.Seed(fingerprint, "exact-vat-01");

        var result = await BuildMatcher(supplierStore: supplierStore).MatchAsync(
            new InvoiceParseResult
            {
                SupplierName      = "Acme B.V.",
                SupplierVatNumber = "NL123456789B01"
            },
            CancellationToken.None);

        Assert.True(result.IsMatched);
        Assert.True(result.RequiresReview);
        Assert.Equal("exact-vat-01", result.ExactSupplierId);
        Assert.Equal(SupplierMatchSources.Vat, result.MatchedBy);
    }

    [Fact]
    public async Task MatchAsync_ShouldPreferKvk_WhenBothKvkAndVatAreMapped()
    {
        var supplierStore = new FakeSupplierMappingStore();
        supplierStore.Seed(new SupplierFingerprintBuilder().BuildKvK("12345678"), "exact-kvk-win");
        supplierStore.Seed(new SupplierFingerprintBuilder().BuildVat("NL123456789B01"), "exact-vat-lose");

        var result = await BuildMatcher(supplierStore: supplierStore).MatchAsync(
            new InvoiceParseResult
            {
                SupplierName      = "Acme B.V.",
                SupplierKvKNumber = "12345678",
                SupplierVatNumber = "NL123456789B01"
            },
            CancellationToken.None);

        Assert.Equal("exact-kvk-win", result.ExactSupplierId);
        Assert.Equal(SupplierMatchSources.KvK, result.MatchedBy);
    }

    [Fact]
    public async Task MatchAsync_ShouldPreferKvk_WhenBothKvkAndIbanAreMapped()
    {
        var supplierStore = new FakeSupplierMappingStore();
        supplierStore.Seed(new SupplierFingerprintBuilder().BuildKvK("12345678"), "exact-kvk-win");

        var bankStore = new FakeBankAccountMappingStore();
        bankStore.Seed("IBAN:NL91ABNA0417164300", "exact-iban-lose");

        var result = await BuildMatcher(supplierStore: supplierStore, bankStore: bankStore).MatchAsync(
            new InvoiceParseResult
            {
                SupplierName        = "Acme B.V.",
                SupplierKvKNumber   = "12345678",
                SupplierBankAccount = "NL91 ABNA 0417 1643 00"
            },
            CancellationToken.None);

        Assert.Equal("exact-kvk-win", result.ExactSupplierId);
        Assert.Equal(SupplierMatchSources.KvK, result.MatchedBy);
    }

    [Fact]
    public async Task MatchAsync_ShouldPreferVat_WhenVatAndIbanAreMappedButKvkIsAbsent()
    {
        var supplierStore = new FakeSupplierMappingStore();
        supplierStore.Seed(new SupplierFingerprintBuilder().BuildVat("NL123456789B01"), "exact-vat-win");

        var bankStore = new FakeBankAccountMappingStore();
        bankStore.Seed("IBAN:NL91ABNA0417164300", "exact-iban-lose");

        var result = await BuildMatcher(supplierStore: supplierStore, bankStore: bankStore).MatchAsync(
            new InvoiceParseResult
            {
                SupplierName        = "Acme B.V.",
                SupplierVatNumber   = "NL123456789B01",
                SupplierBankAccount = "NL91 ABNA 0417 1643 00"
            },
            CancellationToken.None);

        Assert.Equal("exact-vat-win", result.ExactSupplierId);
        Assert.Equal(SupplierMatchSources.Vat, result.MatchedBy);
    }

    [Fact]
    public async Task MatchAsync_ShouldSkipKvkCheck_WhenKvkNumberIsNull()
    {
        var supplierStore = new FakeSupplierMappingStore();
        supplierStore.Seed("KVK:", "should-never-be-returned");

        var result = await BuildMatcher(supplierStore: supplierStore).MatchAsync(
            new InvoiceParseResult { SupplierName = "Acme B.V.", SupplierKvKNumber = null },
            CancellationToken.None);

        Assert.False(result.IsMatched);
    }

    [Fact]
    public async Task MatchAsync_ShouldSkipVatCheck_WhenVatNumberIsNull()
    {
        var supplierStore = new FakeSupplierMappingStore();
        supplierStore.Seed("VAT:", "should-never-be-returned");

        var result = await BuildMatcher(supplierStore: supplierStore).MatchAsync(
            new InvoiceParseResult { SupplierName = "Acme B.V.", SupplierVatNumber = null },
            CancellationToken.None);

        Assert.False(result.IsMatched);
    }
}

file sealed class FakeSupplierMappingStore : ISupplierMappingStore
{
    private readonly Dictionary<string, string> _data = new();

    public void Seed(string fingerprint, string exactSupplierId) => _data[fingerprint] = exactSupplierId;

    public Task<string?> FindExactSupplierIdAsync(string fingerprint, CancellationToken cancellationToken = default)
        => Task.FromResult(_data.TryGetValue(fingerprint, out var id) ? (string?)id : null);

    public Task SaveAsync(string fingerprint, string exactSupplierId, CancellationToken cancellationToken = default)
    {
        _data[fingerprint] = exactSupplierId;
        return Task.CompletedTask;
    }
}

file sealed class FakeBankAccountMappingStore : IBankAccountMappingStore
{
    private readonly Dictionary<string, string> _data = new();

    public void Seed(string fingerprint, string exactSupplierId) => _data[fingerprint] = exactSupplierId;

    public Task<string?> FindExactSupplierIdAsync(string fingerprint, CancellationToken cancellationToken = default)
        => Task.FromResult(_data.TryGetValue(fingerprint, out var id) ? (string?)id : null);

    public Task SaveAsync(string fingerprint, string exactSupplierId, CancellationToken cancellationToken = default)
    {
        _data[fingerprint] = exactSupplierId;
        return Task.CompletedTask;
    }
}
