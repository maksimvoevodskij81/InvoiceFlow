using InvoiceFlow.Api.Features.Exact;

namespace InvoiceFlow.Api.Tests.Fakes;

public sealed class FakeExactPostOutboxWriter : IExactPostOutboxWriter
{
    public int CallsCount { get; private set; }

    public string? LastInvoiceId { get; private set; }

    public Task EnqueueAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceId);

        CallsCount++;
        LastInvoiceId = invoiceId;

        return Task.CompletedTask;
    }
}