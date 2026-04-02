namespace InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

public sealed class BusinessDuplicateCheckResult
{
    public bool IsDuplicate { get; set; }
    public string DuplicateKey { get; set; } = string.Empty;
    public string ExistingInvoiceId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}