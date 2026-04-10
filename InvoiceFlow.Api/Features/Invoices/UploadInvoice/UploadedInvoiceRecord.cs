namespace InvoiceFlow.Api.Features.Invoices.UploadInvoice;

public sealed class UploadedInvoiceRecord
{
    public required string InvoiceId { get; init; }

    public required string OriginalFileName { get; init; }

    public required string StoredFilePath { get; init; }

    public required string Status { get; set; }

    public string? Message { get; set; }

    public DateTime CreatedAtUtc { get; init; }

    public required string FileHash { get; init; }

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
    public string? ExactPostingStatus { get; set; }

    public string? ExactDocumentId { get; set; }

    public DateTime? PostedToExactAtUtc { get; set; }

    public string? ExactPostingError { get; set; }
}