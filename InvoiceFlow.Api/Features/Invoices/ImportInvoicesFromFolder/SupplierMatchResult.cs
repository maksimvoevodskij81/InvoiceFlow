namespace InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

public sealed class SupplierMatchResult
{
    public bool IsMatched { get; set; }
    public bool RequiresReview { get; set; }
    public string MatchedBy { get; set; } = string.Empty;
    public string InternalSupplierId { get; set; } = string.Empty;
    public string ExactSupplierId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}