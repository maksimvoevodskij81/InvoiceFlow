namespace InvoiceFlow.Api.Features.Invoices.RetryExtraction;

public sealed class RetryExtractionResponse
{
    public required string InvoiceId { get; init; }
    public required string Status { get; init; }
    public required string Message { get; init; }
}
