namespace InvoiceFlow.Api.Features.Invoices.Extraction;

public sealed class ExtractionMetadata
{
    public string ModelName { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime CompletedAtUtc { get; set; }
    public bool IsSuccessful { get; set; }
    public List<ExtractionWarning> Warnings { get; set; } = new();
    public ExtractionError? Error { get; set; }
}
