namespace InvoiceFlow.Api.Features.Invoices.GetInvoiceStatus;

public sealed class GetInvoiceStatusResponse
{
    public string InvoiceId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? SupplierName { get; set; }

    public string? InvoiceNumber { get; set; }

    public DateOnly? InvoiceDate { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? Currency { get; set; }

    public bool IsSupplierMatched { get; set; }

    public bool RequiresSupplierReview { get; set; }

    public string? SupplierMatchedBy { get; set; }

    public string? InternalSupplierId { get; set; }

    public string? ExactSupplierId { get; set; }

    public string? SupplierMatchMessage { get; set; }
}