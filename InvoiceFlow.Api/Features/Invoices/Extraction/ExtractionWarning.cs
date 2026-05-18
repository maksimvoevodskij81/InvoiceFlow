namespace InvoiceFlow.Api.Features.Invoices.Extraction;

public sealed class ExtractionWarning
{
    public ExtractionWarningType Type { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
