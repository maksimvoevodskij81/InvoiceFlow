
namespace InvoiceFlow.Api.Features.Invoices.UploadInvoice;

public interface IInvoiceUploadService
{
    Task<UploadInvoiceAcceptedResponse> UploadAsync(IFormFile file, string? uploadedBy = null, CancellationToken cancellationToken = default);
}