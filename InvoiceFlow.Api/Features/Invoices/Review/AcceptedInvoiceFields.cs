namespace InvoiceFlow.Api.Features.Invoices.Review;

public sealed record AcceptedInvoiceFields
{
    public string?   SupplierName  { get; init; }
    public string?   InvoiceNumber { get; init; }
    public DateOnly? InvoiceDate   { get; init; }
    public decimal?  TotalAmount   { get; init; }
    public string?   Currency      { get; init; }
}
