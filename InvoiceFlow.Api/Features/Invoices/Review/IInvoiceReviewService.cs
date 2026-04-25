namespace InvoiceFlow.Api.Features.Invoices.Review;

public interface IInvoiceReviewService
{
    Task ApproveAsync(string invoiceId, string? reviewComment, CancellationToken cancellationToken = default);

    Task RejectAsync(string invoiceId, string? reviewComment, CancellationToken cancellationToken = default);
}
