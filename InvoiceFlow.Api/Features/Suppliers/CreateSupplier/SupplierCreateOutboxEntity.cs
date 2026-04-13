namespace InvoiceFlow.Api.Features.Suppliers.CreateSupplier;

public sealed class SupplierCreateOutboxEntity
{
    public Guid Id { get; set; }
    public string InvoiceId { get; set; } = string.Empty;
    public string Status { get; set; } = SupplierCreateOutboxStatuses.Pending;
    public int AttemptCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
    public string? CreatedExactSupplierId { get; set; }
    public string? LastError { get; set; }
}