using InvoiceFlow.Api.Features.Invoices.Extraction;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

namespace InvoiceFlow.Api.Infrastructure;

public sealed class LlmInvoiceParser : IInvoiceParser
{
    private readonly ILlmInvoiceExtractor _extractor;

    public LlmExtractionResult? LastExtractionResult { get; private set; }

    public LlmInvoiceParser(ILlmInvoiceExtractor extractor)
    {
        ArgumentNullException.ThrowIfNull(extractor);
        _extractor = extractor;
    }

    public async Task<InvoiceParseResult> ParseAsync(FolderInvoiceFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        try
        {
            var extractionResult = await _extractor.ExtractAsync(file, cancellationToken);
            LastExtractionResult = extractionResult;

            // If extraction failed or fields are null, return empty result
            if (!extractionResult.IsSuccessful || extractionResult.Fields == null)
            {
                return new InvoiceParseResult();
            }

            // Map extracted fields to InvoiceParseResult
            var parseResult = new InvoiceParseResult
            {
                SupplierName = extractionResult.Fields.SupplierName ?? string.Empty,
                InvoiceNumber = extractionResult.Fields.InvoiceNumber ?? string.Empty,
                InvoiceDate = extractionResult.Fields.InvoiceDate,
                TotalAmount = extractionResult.Fields.TotalAmount,
                Currency = extractionResult.Fields.Currency ?? string.Empty,
                SupplierAddressLine = extractionResult.Fields.SupplierAddressLine,
                SupplierPostcode = extractionResult.Fields.SupplierPostcode,
                SupplierCity = extractionResult.Fields.SupplierCity,
                SupplierCountry = extractionResult.Fields.SupplierCountry,
                SupplierBankAccount = extractionResult.Fields.SupplierBankAccount,
                SupplierBicCode = extractionResult.Fields.SupplierBicCode,
                SupplierVatNumber = extractionResult.Fields.SupplierVatNumber,
                SupplierKvKNumber = extractionResult.Fields.SupplierKvKNumber
            };

            return parseResult;
        }
        catch (Exception ex)
        {
            // Catch unexpected extractor exceptions
            var failedResult = new LlmExtractionResult
            {
                IsSuccessful = false,
                Raw = new LlmRawExtractionResult
                {
                    RawJson = null,
                    ModelName = "Unknown",
                    ExtractedAtUtc = DateTime.UtcNow,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message
                },
                Fields = null,
                Metadata = new ExtractionMetadata
                {
                    ModelName = "Unknown",
                    StartedAtUtc = DateTime.UtcNow,
                    CompletedAtUtc = DateTime.UtcNow,
                    IsSuccessful = false,
                    Warnings = new(),
                    Error = new ExtractionError
                    {
                        Code = "EXTRACTION_EXCEPTION",
                        Message = ex.Message,
                        IsRetryable = true
                    }
                }
            };

            LastExtractionResult = failedResult;
            return new InvoiceParseResult();
        }
    }
}
