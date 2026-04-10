using InvoiceFlow.Api.Features.Exact;

namespace InvoiceFlow.Api.Tests.Fakes;

public sealed class FakeExactPostOutboxWriter : IExactPostOutboxWriter
{
    public int EnqueueCallsCount { get; private set; }

    public string? LastEnqueuedInvoiceId { get; private set; }

    public int RequeueCallsCount { get; private set; }

    public string? LastRequeuedInvoiceId { get; private set; }

    public Task EnqueueAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceId);

        EnqueueCallsCount++;
        LastEnqueuedInvoiceId = invoiceId;

        return Task.CompletedTask;
    }

    public Task RequeueAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceId);

        RequeueCallsCount++;
        LastRequeuedInvoiceId = invoiceId;

        return Task.CompletedTask;
    }
}