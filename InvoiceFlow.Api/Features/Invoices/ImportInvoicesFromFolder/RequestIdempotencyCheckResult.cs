namespace InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

public sealed class RequestIdempotencyCheckResult
{
    public bool IsDuplicateRequest { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string ExistingInvoiceId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}