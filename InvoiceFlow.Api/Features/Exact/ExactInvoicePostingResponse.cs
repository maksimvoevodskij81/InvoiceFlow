namespace InvoiceFlow.Api.Features.Exact
{
    public sealed class ExactInvoicePostingResponse
    {
        public bool IsSuccess { get; set; }
        public string? ExactDocumentId { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
