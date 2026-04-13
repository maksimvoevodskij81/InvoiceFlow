namespace InvoiceFlow.Api.Features.Suppliers.CreateSupplier;

public interface ISupplierCreateOutboxWriter
{
    Task EnqueueAsync(string invoiceId, CancellationToken cancellationToken = default);
    Task RequeueAsync(string invoiceId, CancellationToken cancellationToken = default);
}