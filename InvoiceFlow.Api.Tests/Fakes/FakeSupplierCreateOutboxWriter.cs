using InvoiceFlow.Api.Features.Suppliers.CreateSupplier;

namespace InvoiceFlow.Api.Tests.Fakes;

public sealed class FakeSupplierCreateOutboxWriter : ISupplierCreateOutboxWriter
{
    public List<string> EnqueuedInvoiceIds { get; } = new();
    public List<string> RequeuedInvoiceIds { get; } = new();

    public int EnqueueCallsCount
    {
        get
        {
            return EnqueuedInvoiceIds.Count;
        }
    }

    public int RequeueCallsCount
    {
        get
        {
            return RequeuedInvoiceIds.Count;
        }
    }

    public string? LastEnqueuedInvoiceId
    {
        get
        {
            return EnqueuedInvoiceIds.LastOrDefault();
        }
    }

    public string? LastRequeuedInvoiceId
    {
        get
        {
            return RequeuedInvoiceIds.LastOrDefault();
        }
    }

    public Task EnqueueAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        EnqueuedInvoiceIds.Add(invoiceId);
        return Task.CompletedTask;
    }

    public Task RequeueAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        RequeuedInvoiceIds.Add(invoiceId);
        return Task.CompletedTask;
    }
}