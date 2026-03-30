namespace InvoiceFlow.Api.Features.Invoices.UploadInvoice;

public sealed class UploadInvoiceRequest
{
    public IFormFile File { get; set; } = default!;
}