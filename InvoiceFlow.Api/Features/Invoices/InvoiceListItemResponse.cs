namespace InvoiceFlow.Api.Features.Invoices;

public sealed class InvoiceListItemResponse
{
    public required string InvoiceId { get; init; }
    public required string Status { get; init; }
    public string? SupplierName { get; init; }
    public string? InvoiceNumber { get; init; }
    public DateOnly? InvoiceDate { get; init; }
    public decimal? TotalAmount { get; init; }
    public string? Currency { get; init; }
    public bool RequiresSupplierReview { get; init; }
    public bool CanCreateSupplier { get; init; }
    public string? ReviewDecision { get; init; }
}
