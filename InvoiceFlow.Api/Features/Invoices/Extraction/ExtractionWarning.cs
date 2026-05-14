namespace InvoiceFlow.Api.Features.Invoices.Extraction;

public sealed class ExtractionWarning
{
    public string FieldName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ExtractionWarningType Type { get; set; }
}
