namespace InvoiceFlow.Api.Features.Invoices.Extraction;

public sealed class ClaudePromptBuilder
{
    private const string SystemPrompt =
        """
        You are an invoice data extraction assistant.
        Extract structured data from the invoice text and return ONLY a strict JSON object with no extra text, no markdown, no code fences.

        Return a JSON object with exactly these fields (use null for missing values):
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
        - Return valid JSON only. No explanation, no prose.
        - Dates must be in ISO 8601 format (YYYY-MM-DD).
        - Amounts must be numeric (no currency symbols).
        - If a field cannot be found, use null.
        """;

    public ClaudePrompt Build(string invoiceText)
    {
        return new ClaudePrompt
        {
            SystemPrompt = SystemPrompt,
            UserMessage = invoiceText
        };
    }
}
