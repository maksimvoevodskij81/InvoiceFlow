namespace InvoiceFlow.Api.Features.Exact
{
    public sealed class ExactPostResult
    {
        public bool Success { get; init; }

        public string? ExternalDocumentId { get; init; }

        public string? ErrorMessage { get; init; }
    }
}
