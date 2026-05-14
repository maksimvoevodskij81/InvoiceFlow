namespace InvoiceFlow.Api.Features.Invoices.Extraction;

public sealed class LlmExtractedFields
{
    public string? SupplierName { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateOnly? InvoiceDate { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Currency { get; set; }
    public string? SupplierAddressLine { get; set; }
    public string? SupplierPostcode { get; set; }
    public string? SupplierCity { get; set; }
    public string? SupplierCountry { get; set; }
    public string? SupplierBankAccount { get; set; }
    public string? SupplierBicCode { get; set; }
    public string? SupplierVatNumber { get; set; }
    public string? SupplierKvKNumber { get; set; }
}
