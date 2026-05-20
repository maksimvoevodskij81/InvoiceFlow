namespace InvoiceFlow.Api.Features.Suppliers.Idempotency;

public sealed class BankAccountFingerprintBuilder
{
    public string Build(string? bankAccount)
    {
        if (string.IsNullOrWhiteSpace(bankAccount))
        {
            return string.Empty;
        }

        string normalized = bankAccount
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim()
            .ToUpperInvariant();

        // IBANs start with a 2-letter country code followed by 2 check digits.
        // Everything else (local account numbers, Indian formats, etc.) uses BANKACCOUNT prefix.
        bool isIban = normalized.Length >= 4
            && char.IsLetter(normalized[0])
            && char.IsLetter(normalized[1])
            && char.IsDigit(normalized[2])
            && char.IsDigit(normalized[3]);

        return isIban ? $"IBAN:{normalized}" : $"BANKACCOUNT:{normalized}";
    }
}