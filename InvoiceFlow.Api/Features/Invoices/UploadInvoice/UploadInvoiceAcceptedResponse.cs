namespace InvoiceFlow.Api.Features.Invoices.UploadInvoice;

public sealed class UploadInvoiceAcceptedResponse
{
    public string InvoiceId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<string> MissingFields { get; set; } = new();
}
