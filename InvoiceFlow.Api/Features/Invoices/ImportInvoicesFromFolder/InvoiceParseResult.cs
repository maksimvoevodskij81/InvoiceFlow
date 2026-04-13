namespace InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

public sealed class InvoiceParseResult
{
    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly? InvoiceDate { get; set; }
    public decimal? TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;

    public string? SupplierAddressLine { get; set; }
    public string? SupplierPostcode { get; set; }
    public string? SupplierCity { get; set; }
    public string? SupplierCountry { get; set; }
    public string? SupplierBankAccount { get; set; }
    public string? SupplierBicCode { get; set; }

    public string? SupplierVatNumber { get; set; }
    public string? SupplierKvKNumber { get; set; }
}