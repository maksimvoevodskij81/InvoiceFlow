namespace InvoiceFlow.Api.Features.Invoices.Review;

public interface IInvoiceReviewService
{
    Task ApproveAsync(string invoiceId, string? reviewComment, AcceptedInvoiceFields? acceptedFields = null, string? reviewedBy = null, CancellationToken cancellationToken = default);

    Task RejectAsync(string invoiceId, string? reviewComment, string? reviewedBy = null, CancellationToken cancellationToken = default);
}
