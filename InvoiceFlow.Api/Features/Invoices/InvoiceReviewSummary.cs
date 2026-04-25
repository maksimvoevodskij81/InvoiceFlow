namespace InvoiceFlow.Api.Features.Invoices;

public sealed class InvoiceReviewSummary
{
    public bool RequiresReview { get; set; }

    public bool CanCreateSupplier { get; set; }

    public bool HasNewBankDetails { get; set; }

    public List<string> Reasons { get; set; } = new();

    public string? CurrentDecisionMessage { get; set; }
}
