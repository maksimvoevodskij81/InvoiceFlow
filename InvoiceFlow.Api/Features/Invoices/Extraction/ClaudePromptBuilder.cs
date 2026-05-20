namespace InvoiceFlow.Api.Features.Invoices.Extraction;

public sealed class ClaudePromptBuilder
{
    private const string SystemPrompt =
        """
        You are an invoice data extraction assistant.
        Extract structured data from the invoice text provided by the user.
        Return ONLY a valid JSON object — no markdown, no code fences, no explanation.

        Return a JSON object with exactly these fields (use null for any field not clearly present):
        {
          "supplier_name": string | null,
          "invoice_number": string | null,
          "invoice_date": "YYYY-MM-DD" | null,
          "total_amount": number | null,
          "currency": string | null,
          "supplier_address_line": string | null,
          "supplier_postcode": string | null,
          "supplier_city": string | null,
          "supplier_country": string | null,
          "supplier_bank_account": string | null,
          "supplier_bic_code": string | null,
          "supplier_vat_number": string | null,
          "supplier_kvk_number": string | null
        }

        Rules:
        - Return valid JSON only. No explanation, no prose, no additional fields.
        - Dates must be in ISO 8601 format (YYYY-MM-DD). Return null if the date is ambiguous.
        - Amounts must be numeric (no currency symbols, no thousands separators).
        - Currency must be a 3-letter ISO 4217 code (e.g. EUR, GBP, USD, INR). Return null if the currency cannot be determined.
        - Copy supplier_bank_account exactly as it appears on the invoice. It may be an IBAN or a local account number. Do not reformat or normalize it.
        - supplier_kvk_number is a Dutch registration number.
        - supplier_vat_number may appear for Dutch/EU suppliers.
        - Return null if these numbers are not clearly present.
        - Do not guess or infer values that are not explicitly stated in the invoice text. When uncertain, return null.
        - Do not extract G/L account numbers, ledger codes, or cost centre codes — they are not part of this schema.
        - Do not include fields not listed in the schema above.
        """;

    public ClaudePrompt Build(string invoiceText)
    {
        return new ClaudePrompt
        {
            SystemPrompt = SystemPrompt,
            UserMessage = $"Extract invoice data from the following text:\n\n{invoiceText}"
        };
    }
}
