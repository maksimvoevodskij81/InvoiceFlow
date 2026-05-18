using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

namespace InvoiceFlow.Api.Features.Invoices.Extraction;

public sealed class LlmInvoiceParser : IInvoiceParser
{
    private readonly ILlmInvoiceExtractor _extractor;

    public LlmExtractionResult? LastExtractionResult { get; private set; }

    public LlmInvoiceParser(ILlmInvoiceExtractor extractor)
    {
        _extractor = extractor;
    }

    public async Task<InvoiceParseResult> ParseAsync(FolderInvoiceFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        LlmExtractionResult result;
        try
        {
            result = await _extractor.ExtractAsync(file, cancellationToken);
        }
        catch (Exception ex)
        {
            result = new LlmExtractionResult
            {
                IsSuccessful = false,
                Raw = new LlmRawExtractionResult { RawJson = null },
                Fields = null,
                Metadata = new ExtractionMetadata
                {
                    Model = "unknown",
                    ExtractedAtUtc = DateTime.UtcNow,
                    Warnings = []
                },
                Error = new ExtractionError
                {
                    Code = "UnexpectedException",
                    Message = ex.Message
                }
            };
        }

        LastExtractionResult = result;

        if (!result.IsSuccessful || result.Fields is null)
        {
            return new InvoiceParseResult();
        }

        return MapToParseResult(result.Fields);
    }

    private static InvoiceParseResult MapToParseResult(LlmExtractedFields fields)
    {
        return new InvoiceParseResult
        {
            SupplierName = fields.SupplierName ?? string.Empty,
            InvoiceNumber = fields.InvoiceNumber ?? string.Empty,
            Currency = fields.Currency ?? string.Empty,
            InvoiceDate = fields.InvoiceDate,
            TotalAmount = fields.TotalAmount,
            SupplierAddressLine = fields.SupplierAddressLine,
            SupplierPostcode = fields.SupplierPostcode,
            SupplierCity = fields.SupplierCity,
            SupplierCountry = fields.SupplierCountry,
            SupplierBankAccount = fields.SupplierBankAccount,
            SupplierBicCode = fields.SupplierBicCode,
            SupplierVatNumber = fields.SupplierVatNumber,
            SupplierKvKNumber = fields.SupplierKvKNumber
        };
    }
}
