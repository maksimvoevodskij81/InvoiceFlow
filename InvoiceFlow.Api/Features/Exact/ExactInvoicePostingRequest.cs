namespace InvoiceFlow.Api.Features.Exact
{
    public sealed class ExactInvoicePostingRequest
    {
        public string ExactSupplierId { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateOnly? InvoiceDate { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal? TotalAmount { get; set; }
        public string? Description { get; set; }
    }
}
