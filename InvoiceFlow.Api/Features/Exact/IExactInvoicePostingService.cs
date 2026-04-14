namespace InvoiceFlow.Api.Features.Exact;

using InvoiceFlow.Api.Features.Invoices.UploadInvoice;

public interface IExactInvoicePostingService
{
    Task<ExactInvoicePostingResponse> PostAsync(ExactInvoicePostingRequest request, CancellationToken cancellationToken = default);
}

