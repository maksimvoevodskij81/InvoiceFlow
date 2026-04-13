namespace InvoiceFlow.Api.Features.Suppliers.CreateSupplier;

public sealed class FakeSupplierCreator : ISupplierCreator
{
    public Task<string> CreateAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"exact-created-{invoiceId}");
    }
}