using InvoiceFlow.Api.Features.Suppliers.Idempotency;

namespace InvoiceFlow.Api.Tests.Features.Suppliers.Idempotency;

public sealed class BankAccountFingerprintBuilderTests
{
    private readonly BankAccountFingerprintBuilder _builder = new();

    // --- IBAN inputs ---

    [Fact]
    public void Build_ShouldReturnIbanFingerprint_ForNlIbanWithSpaces()
    {
        var result = _builder.Build("NL91 ABNA 0417 1643 00");
        Assert.Equal("IBAN:NL91ABNA0417164300", result);
    }

    [Fact]
    public void Build_ShouldReturnIbanFingerprint_ForNlIbanWithoutSpaces()
    {
        var result = _builder.Build("NL91ABNA0417164300");
        Assert.Equal("IBAN:NL91ABNA0417164300", result);
    }

    [Fact]
    public void Build_ShouldReturnIbanFingerprint_ForNlIbanLowercase()
    {
        var result = _builder.Build("nl91 abna 0417 1643 00");
        Assert.Equal("IBAN:NL91ABNA0417164300", result);
    }

    [Fact]
    public void Build_ShouldReturnIbanFingerprint_ForGbIbanWithSpaces()
    {
        var result = _builder.Build("GB29 NWBK 6016 1331 9268 19");
        Assert.Equal("IBAN:GB29NWBK60161331926819", result);
    }

    // --- Non-IBAN (local account numbers) ---

    [Fact]
    public void Build_ShouldReturnBankAccountFingerprint_ForIndianAccountNumber()
    {
        var result = _builder.Build("50200012345678");
        Assert.Equal("BANKACCOUNT:50200012345678", result);
    }

    [Fact]
    public void Build_ShouldReturnBankAccountFingerprint_ForIndianAccountNumberWithSpaces()
    {
        var result = _builder.Build("502 000 123 456 78");
        Assert.Equal("BANKACCOUNT:50200012345678", result);
    }

    // --- Empty/null ---

    [Fact]
    public void Build_ShouldReturnEmpty_ForNull()
    {
        var result = _builder.Build(null);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Build_ShouldReturnEmpty_ForWhitespace()
    {
        var result = _builder.Build("   ");
        Assert.Equal(string.Empty, result);
    }
}
