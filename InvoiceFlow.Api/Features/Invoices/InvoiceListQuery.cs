namespace InvoiceFlow.Api.Features.Invoices;

public sealed class InvoiceListQuery
{
    public string? Status { get; init; }
    public string? ReviewDecision { get; init; }
    public bool? CanCreateSupplier { get; init; }
}
