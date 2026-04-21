namespace InvoiceFlow.Api.Features.Invoices.Review;

public interface IInvoiceReviewService
{
    Task ApproveAsync(string invoiceId, CancellationToken cancellationToken = default);
}
