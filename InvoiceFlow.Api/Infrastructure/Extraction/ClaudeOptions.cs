namespace InvoiceFlow.Api.Infrastructure.Extraction;

public sealed class ClaudeOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-sonnet-4-6";
    public int MaxTokens { get; set; } = 1024;
    public string AnthropicVersion { get; set; } = "2023-06-01";
}
