namespace InvoiceFlow.Api.Features.Invoices.Extraction;

public sealed class LlmRawExtractionResult
{
    public string? RawJson { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public DateTime ExtractedAtUtc { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
}
