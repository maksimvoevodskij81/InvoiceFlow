namespace InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

public sealed class ImportInvoicesFromFolderResponse
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly? InvoiceDate { get; set; }
    public decimal? TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsSupplierMatched { get; set; }
    public bool RequiresSupplierReview { get; set; }
    public string SupplierMatchedBy { get; set; } = string.Empty;
    public string InternalSupplierId { get; set; } = string.Empty;
    public string ExactSupplierId { get; set; } = string.Empty;
    public string SupplierMatchMessage { get; set; } = string.Empty;
}