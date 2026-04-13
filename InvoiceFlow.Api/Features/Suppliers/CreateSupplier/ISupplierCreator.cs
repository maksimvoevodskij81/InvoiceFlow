namespace InvoiceFlow.Api.Features.Suppliers.CreateSupplier;

public interface ISupplierCreator
{
    Task<string> CreateAsync(SupplierCreateRequest request, CancellationToken cancellationToken = default);
}