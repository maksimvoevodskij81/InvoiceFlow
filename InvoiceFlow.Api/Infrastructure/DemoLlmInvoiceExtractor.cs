using InvoiceFlow.Api.Features.Invoices.Extraction;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

namespace InvoiceFlow.Api.Infrastructure;

public sealed class DemoLlmInvoiceExtractor : ILlmInvoiceExtractor
{
    public Task<LlmExtractionResult> ExtractAsync(FolderInvoiceFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        var startedAtUtc = DateTime.UtcNow;
        var completedAtUtc = DateTime.UtcNow;

        var fields = new LlmExtractedFields
        {
            SupplierName = "Demo Supplier Ltd.",
            InvoiceNumber = "INV-2026-001",
            InvoiceDate = new DateOnly(2026, 4, 15),
            TotalAmount = 1250.50m,
            Currency = "EUR",
            SupplierAddressLine = "123 Demo Street",
            SupplierPostcode = "12345",
            SupplierCity = "Amsterdam",
            SupplierCountry = "NL",
            SupplierBankAccount = "NL91ABNA0417164300",
            SupplierBicCode = "ABNANL2A",
            SupplierVatNumber = "NL123456789B01",
            SupplierKvKNumber = "12345678"
        };

        var metadata = new ExtractionMetadata
        {
            ModelName = "Demo-LLM-v1",
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = completedAtUtc,
            IsSuccessful = true,
            Warnings = new(),
            Error = null
        };

        var result = new LlmExtractionResult
        {
            IsSuccessful = true,
            Raw = new LlmRawExtractionResult
            {
                RawJson = null,
                ModelName = "Demo-LLM-v1",
                ExtractedAtUtc = completedAtUtc,
                IsSuccessful = true,
                ErrorMessage = null
            },
            Fields = fields,
            Metadata = metadata
        };

        return Task.FromResult(result);
    }
}
