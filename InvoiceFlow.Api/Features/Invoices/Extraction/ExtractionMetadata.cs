namespace InvoiceFlow.Api.Features.Invoices.Extraction;

public sealed class ExtractionMetadata
{
    public string Model { get; set; } = string.Empty;
    public DateTime ExtractedAtUtc { get; set; }
    public List<ExtractionWarning> Warnings { get; set; } = [];
}
