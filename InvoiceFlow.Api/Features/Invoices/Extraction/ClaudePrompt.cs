namespace InvoiceFlow.Api.Features.Invoices.Extraction;

public sealed class ClaudePrompt
{
    public required string SystemPrompt { get; init; }
    public required string UserMessage { get; init; }
}
