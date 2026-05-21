using InvoiceFlow.Api.Features.Suppliers.Idempotency;

namespace InvoiceFlow.Api.Tests.Features.Suppliers.Idempotency;

public sealed class SupplierFingerprintBuilderKvkVatTests
{
    private readonly SupplierFingerprintBuilder _builder = new();

    [Fact]
    public void BuildKvK_ShouldReturnKvkFingerprint_WhenPlainNumber()
    {
        Assert.Equal("KVK:12345678", _builder.BuildKvK("12345678"));
    }

    [Fact]
    public void BuildKvK_ShouldStripSpacesAndNonDigits_WhenFormatted()
    {
        Assert.Equal("KVK:12345678", _builder.BuildKvK("12 34 56 78"));
    }

    [Fact]
    public void BuildKvK_ShouldReturnEmpty_WhenNull()
    {
        Assert.Equal(string.Empty, _builder.BuildKvK(null));
    }

    [Fact]
    public void BuildKvK_ShouldReturnEmpty_WhenWhitespace()
    {
        Assert.Equal(string.Empty, _builder.BuildKvK("   "));
    }

    [Fact]
    public void BuildKvK_ShouldReturnEmpty_WhenNoDigitsPresent()
    {
        Assert.Equal(string.Empty, _builder.BuildKvK("no-digits-here"));
    }

    [Fact]
    public void BuildVat_ShouldReturnVatFingerprint_WhenDutchVat()
    {
        Assert.Equal("VAT:NL123456789B01", _builder.BuildVat("NL123456789B01"));
    }

    [Fact]
    public void BuildVat_ShouldStripSpaces_WhenVatHasSpaces()
    {
        Assert.Equal("VAT:NL123456789B01", _builder.BuildVat("NL 123456789 B01"));
    }

    [Fact]
    public void BuildVat_ShouldUppercase_WhenLowercase()
    {
        Assert.Equal("VAT:GB123456789", _builder.BuildVat("gb123456789"));
    }

    [Fact]
    public void BuildVat_ShouldReturnEmpty_WhenNull()
    {
        Assert.Equal(string.Empty, _builder.BuildVat(null));
    }

    [Fact]
    public void BuildVat_ShouldReturnEmpty_WhenWhitespace()
    {
        Assert.Equal(string.Empty, _builder.BuildVat("   "));
    }
}
