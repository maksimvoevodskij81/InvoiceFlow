namespace InvoiceFlow.Api.Features.Suppliers.Idempotency;

public interface IBankAccountMappingStore
{
    Task<string?> FindExactSupplierIdAsync(string fingerprint, CancellationToken cancellationToken = default);

    Task SaveAsync(
        string fingerprint,
        string exactSupplierId,
        CancellationToken cancellationToken = default);
}