using InvoiceFlow.Api.Features.Invoices.RetryExtraction;

namespace InvoiceFlow.Api.Tests.Fakes;

public sealed class FakeInvoiceRetryService : IInvoiceRetryService
{
    public RetryExtractionResponse Response { get; set; } = new RetryExtractionResponse
    {
        InvoiceId = "retry-123",
        Status    = "ReadyToPost",
        Message   = "Retry succeeded."
    };

    public Exception? RetryException { get; set; }

    public string? LastRetryInvoiceId { get; private set; }

    public Task<RetryExtractionResponse> RetryExtractionAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        LastRetryInvoiceId = invoiceId;

        if (RetryException is not null)
        {
            throw RetryException;
        }

        return Task.FromResult(Response);
    }
}
