namespace InvoiceFlow.Api.Features.Suppliers.Idempotency;

public sealed class BankAccountFingerprintBuilder
{
    public string Build(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
        {
            return string.Empty;
        }

        string normalized = iban
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim()
            .ToUpperInvariant();

        return $"IBAN:{normalized}";
    }
}