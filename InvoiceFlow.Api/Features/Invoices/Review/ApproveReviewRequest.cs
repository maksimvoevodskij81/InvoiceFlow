namespace InvoiceFlow.Api.Features.Invoices.Review;

public sealed class ApproveReviewRequest
{
    public string?                Comment        { get; set; }
    public AcceptedInvoiceFields? AcceptedFields { get; set; }
}
