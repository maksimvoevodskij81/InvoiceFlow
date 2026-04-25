using InvoiceFlow.Api.Features.Invoices.Review;

namespace InvoiceFlow.Api.Tests.Fakes;

public sealed class FakeInvoiceReviewService : IInvoiceReviewService
{
    public int ApproveCallsCount { get; private set; }

    public string? LastApprovedInvoiceId { get; private set; }

    public Exception? ApproveException { get; set; }

    public int RejectCallsCount { get; private set; }

    public string? LastRejectedInvoiceId { get; private set; }

    public Exception? RejectException { get; set; }

    public Task ApproveAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        ApproveCallsCount++;
        LastApprovedInvoiceId = invoiceId;

        if (ApproveException is not null)
        {
            throw ApproveException;
        }

        return Task.CompletedTask;
    }

    public Task RejectAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        RejectCallsCount++;
        LastRejectedInvoiceId = invoiceId;

        if (RejectException is not null)
        {
            throw RejectException;
        }

        return Task.CompletedTask;
    }
}
