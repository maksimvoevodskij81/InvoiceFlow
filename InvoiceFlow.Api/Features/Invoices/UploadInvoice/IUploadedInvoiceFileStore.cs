namespace InvoiceFlow.Api.Features.Invoices.UploadInvoice;

public interface IUploadedInvoiceFileStore
{
    Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);
}