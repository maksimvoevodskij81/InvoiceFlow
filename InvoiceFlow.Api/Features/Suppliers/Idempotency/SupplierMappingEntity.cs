namespace InvoiceFlow.Api.Features.Suppliers.Idempotency
{

    public sealed class SupplierMappingEntity
    {
        public Guid Id { get; set; }
        public string Fingerprint { get; set; } = string.Empty;
        public string ExactSupplierId { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }
}
