namespace InvoiceFlow.Api.Features.Invoices.UploadInvoice;

public sealed class UploadInvoiceResponse
{
    public Guid InvoiceId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}