using InvoiceFlow.Api.Features.Invoices;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Features.Suppliers.Idempotency;

namespace InvoiceFlow.Api.Features.Suppliers.Matching;

public sealed class MappingBasedSupplierMatcher : ISupplierMatcher
{
    private readonly ISupplierMappingStore _supplierMappingStore;
    private readonly IBankAccountMappingStore _bankAccountMappingStore;
    private readonly SupplierFingerprintBuilder _supplierFingerprintBuilder;
    private readonly BankAccountFingerprintBuilder _bankAccountFingerprintBuilder;

    public MappingBasedSupplierMatcher(
        ISupplierMappingStore supplierMappingStore,
        IBankAccountMappingStore bankAccountMappingStore,
        SupplierFingerprintBuilder supplierFingerprintBuilder,
        BankAccountFingerprintBuilder bankAccountFingerprintBuilder)
    {
        _supplierMappingStore = supplierMappingStore;
        _bankAccountMappingStore = bankAccountMappingStore;
        _supplierFingerprintBuilder = supplierFingerprintBuilder;
        _bankAccountFingerprintBuilder = bankAccountFingerprintBuilder;
    }

    public async Task<SupplierMatchResult> MatchAsync(
        InvoiceParseResult parseResult,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parseResult);

        string kvkFingerprint = _supplierFingerprintBuilder.BuildKvK(parseResult.SupplierKvKNumber);
        if (!string.IsNullOrEmpty(kvkFingerprint))
        {
            string? kvkId = await _supplierMappingStore.FindExactSupplierIdAsync(kvkFingerprint, cancellationToken);
            if (!string.IsNullOrWhiteSpace(kvkId))
            {
                return Matched(kvkId, SupplierMatchSources.KvK, "Matched by KvK number.");
            }
        }

        string vatFingerprint = _supplierFingerprintBuilder.BuildVat(parseResult.SupplierVatNumber);
        if (!string.IsNullOrEmpty(vatFingerprint))
        {
            string? vatId = await _supplierMappingStore.FindExactSupplierIdAsync(vatFingerprint, cancellationToken);
            if (!string.IsNullOrWhiteSpace(vatId))
            {
                return Matched(vatId, SupplierMatchSources.Vat, "Matched by VAT number.");
            }
        }

        string bankFingerprint = _bankAccountFingerprintBuilder.Build(parseResult.SupplierBankAccount);
        if (!string.IsNullOrEmpty(bankFingerprint))
        {
            string? exactId = await _bankAccountMappingStore.FindExactSupplierIdAsync(bankFingerprint, cancellationToken);
            if (!string.IsNullOrWhiteSpace(exactId))
            {
                return Matched(exactId, SupplierMatchSources.BankAccount, "Matched by IBAN fingerprint.");
            }
        }

        string namePostcodeFingerprint = _supplierFingerprintBuilder.BuildNamePostcode(parseResult);
        string? namePostcodeId = await _supplierMappingStore.FindExactSupplierIdAsync(namePostcodeFingerprint, cancellationToken);
        if (!string.IsNullOrWhiteSpace(namePostcodeId))
        {
            return Matched(namePostcodeId, SupplierMatchSources.Name, "Matched by name and postcode fingerprint.");
        }

        string nameAddrPostcodeFingerprint = _supplierFingerprintBuilder.BuildNameAddressPostcode(parseResult);
        string? nameAddrPostcodeId = await _supplierMappingStore.FindExactSupplierIdAsync(nameAddrPostcodeFingerprint, cancellationToken);
        if (!string.IsNullOrWhiteSpace(nameAddrPostcodeId))
        {
            return Matched(nameAddrPostcodeId, SupplierMatchSources.Name, "Matched by name, address, and postcode fingerprint.");
        }

        return new SupplierMatchResult { IsMatched = false };
    }

    private static SupplierMatchResult Matched(string exactSupplierId, string matchedBy, string reason)
    {
        return new SupplierMatchResult
        {
            IsMatched = true,
            RequiresReview = true,
            ExactSupplierId = exactSupplierId,
            MatchedBy = matchedBy,
            Message = reason,
            Reasons = new List<string> { reason }
        };
    }
}
