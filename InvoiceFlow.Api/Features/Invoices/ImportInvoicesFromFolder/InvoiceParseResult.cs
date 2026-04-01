namespace InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

public sealed class InvoiceParseResult
{
    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly? InvoiceDate { get; set; }
    public decimal? TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
}