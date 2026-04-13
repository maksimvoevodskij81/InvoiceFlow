namespace InvoiceFlow.Api.Features.Suppliers.CreateSupplier;

public interface ISupplierCreator
{
    Task<string> CreateAsync(string invoiceId, CancellationToken cancellationToken = default);
}