namespace InvoiceFlow.Api.Features.Invoices.Extraction;

public sealed class LlmExtractionResult
{
    public bool IsSuccessful { get; set; }
    public LlmRawExtractionResult Raw { get; set; } = new();
    public LlmExtractedFields? Fields { get; set; }
    public ExtractionMetadata Metadata { get; set; } = new();
}
