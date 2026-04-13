using InvoiceFlow.Api.Features.Suppliers.CreateSupplier;

namespace InvoiceFlow.Api.Tests.Fakes;

public sealed class FakeSupplierCreateOutboxWriter : ISupplierCreateOutboxWriter
{
    public List<string> EnqueuedInvoiceIds { get; } = new();

    public Task EnqueueAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        EnqueuedInvoiceIds.Add(invoiceId);
        return Task.CompletedTask;
    }

    public Task RequeueAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        EnqueuedInvoiceIds.Add(invoiceId);
        return Task.CompletedTask;
    }
}