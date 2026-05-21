namespace InvoiceFlow.Api.Features.Invoices.GetInvoiceDetails;

public sealed class AcceptedInvoiceFieldsResponse
{
    public string?   SupplierName  { get; set; }
    public string?   InvoiceNumber { get; set; }
    public DateOnly? InvoiceDate   { get; set; }
    public decimal?  TotalAmount   { get; set; }
    public string?   Currency      { get; set; }
}
