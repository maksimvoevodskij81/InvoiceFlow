namespace InvoiceFlow.Api.Features.Exact;

public sealed class RetryExactPostResponse
{
    public string InvoiceId { get; set; } = string.Empty;

    public string ExactPostingStatus { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}