namespace InvoiceFlow.Api.Features.Exact;

using InvoiceFlow.Api.Features.Invoices.UploadInvoice;

public interface IExactInvoicePostingService
{
    Task<ExactPostResult> PostAsync(UploadedInvoiceRecord invoice, CancellationToken cancellationToken = default);
}

