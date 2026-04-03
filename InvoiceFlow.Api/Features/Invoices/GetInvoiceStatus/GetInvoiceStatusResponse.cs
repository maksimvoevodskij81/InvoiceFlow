namespace InvoiceFlow.Api.Features.Invoices.GetInvoiceStatus;

public sealed class GetInvoiceStatusResponse
{
    public string InvoiceId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

