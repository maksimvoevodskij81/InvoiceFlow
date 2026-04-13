namespace InvoiceFlow.Api.Features.Suppliers.CreateSupplier;

public sealed class FakeSupplierCreator : ISupplierCreator
{
    public Task<string> CreateAsync(SupplierCreateRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"exact-{request.Name}");
    }
}