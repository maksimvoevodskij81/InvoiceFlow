using InvoiceFlow.Api.Features.Invoices.Review;

namespace InvoiceFlow.Api.Tests.Fakes;

public sealed class FakeInvoiceReviewService : IInvoiceReviewService
{
    public int ApproveCallsCount { get; private set; }

    public string? LastApprovedInvoiceId { get; private set; }

    public string? LastApprovedReviewComment { get; private set; }

    public Exception? ApproveException { get; set; }

    public int RejectCallsCount { get; private set; }

    public string? LastRejectedInvoiceId { get; private set; }

    public string? LastRejectedReviewComment { get; private set; }

    public Exception? RejectException { get; set; }

    public AcceptedInvoiceFields? LastAcceptedFields { get; private set; }

    public string? LastReviewedBy { get; private set; }

    public Task ApproveAsync(string invoiceId, string? reviewComment, AcceptedInvoiceFields? acceptedFields = null, string? reviewedBy = null, CancellationToken cancellationToken = default)
    {
        ApproveCallsCount++;
        LastApprovedInvoiceId = invoiceId;
        LastApprovedReviewComment = reviewComment;
        LastAcceptedFields = acceptedFields;
        LastReviewedBy = reviewedBy;

        if (ApproveException is not null)
        {
            throw ApproveException;
        }

        return Task.CompletedTask;
    }

    public Task RejectAsync(string invoiceId, string? reviewComment, string? reviewedBy = null, CancellationToken cancellationToken = default)
    {
        RejectCallsCount++;
        LastRejectedInvoiceId = invoiceId;
        LastRejectedReviewComment = reviewComment;
        LastReviewedBy = reviewedBy;

        if (RejectException is not null)
        {
            throw RejectException;
        }

        return Task.CompletedTask;
    }
}
