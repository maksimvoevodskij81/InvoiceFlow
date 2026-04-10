
namespace InvoiceFlow.Api.Features.Invoices.UploadInvoice;

public interface IInvoiceUploadService
{
    Task<UploadInvoiceAcceptedResponse> UploadAsync(IFormFile file, CancellationToken cancellationToken = default);
}