using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

namespace InvoiceFlow.Api.Features.Invoices.Extraction;

public sealed class DemoLlmInvoiceExtractor : ILlmInvoiceExtractor
{
    public Task<LlmExtractionResult> ExtractAsync(FolderInvoiceFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        var result = new LlmExtractionResult
        {
            IsSuccessful = true,
            Raw = new LlmRawExtractionResult
            {
                RawJson = """{"supplier_name":"Demo Supplier","invoice_number":"INV-001","invoice_date":"2026-04-01","total_amount":123.45,"currency":"EUR"}"""
            },
            Fields = new LlmExtractedFields
            {
                SupplierName = "Demo Supplier",
                InvoiceNumber = "INV-001",
                InvoiceDate = new DateOnly(2026, 4, 1),
                TotalAmount = 123.45m,
                Currency = "EUR"
            },
            Metadata = new ExtractionMetadata
            {
                Model = "demo",
                ExtractedAtUtc = DateTime.UtcNow,
                Warnings = []
            },
            Error = null
        };

        return Task.FromResult(result);
    }
}
