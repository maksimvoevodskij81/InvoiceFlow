namespace InvoiceFlow.Api.Features.Invoices.Extraction;

public sealed class ExtractionError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
