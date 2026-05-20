using InvoiceFlow.Api.Features.Invoices.Extraction;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvoiceFlow.Api.Infrastructure.Extraction;

public sealed class ClaudeInvoiceExtractor : ILlmInvoiceExtractor
{
    private readonly IInvoiceTextExtractor _textExtractor;
    private readonly ClaudePromptBuilder _promptBuilder;
    private readonly HttpClient _httpClient;
    private readonly ClaudeOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public ClaudeInvoiceExtractor(
        IInvoiceTextExtractor textExtractor,
        ClaudePromptBuilder promptBuilder,
        HttpClient httpClient,
        IOptions<ClaudeOptions> options)
    {
        _textExtractor = textExtractor;
        _promptBuilder = promptBuilder;
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<LlmExtractionResult> ExtractAsync(FolderInvoiceFile file, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return Failure("MissingApiKey", "Claude API key is not configured.", isRetryable: false);
        }

        try
        {
            var text = await _textExtractor.ExtractTextAsync(file, cancellationToken);
            var prompt = _promptBuilder.Build(text);

            var requestBody = new ClaudeRequestBody
            {
                Model = _options.Model,
                MaxTokens = _options.MaxTokens,
                System = prompt.SystemPrompt,
                Messages = [new ClaudeMessage { Role = "user", Content = prompt.UserMessage }]
            };

            var requestJson = JsonSerializer.Serialize(requestBody, JsonOptions);
            using var httpContent = new StringContent(requestJson, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages") { Content = httpContent };
            request.Headers.Add("x-api-key", _options.ApiKey);
            request.Headers.Add("anthropic-version", _options.AnthropicVersion);

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                int statusCode = (int)response.StatusCode;
                bool isRetryable = statusCode == 429 || statusCode >= 500;
                return Failure("HttpError", $"Claude API returned HTTP {statusCode}.", isRetryable);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            ClaudeResponseBody? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<ClaudeResponseBody>(responseJson, JsonOptions);
            }
            catch (JsonException ex)
            {
                return Failure("EnvelopeParseError", ex.Message, isRetryable: false);
            }

            var rawText = envelope?.Content?.FirstOrDefault()?.Text;

            if (rawText is null)
            {
                return Failure("EmptyResponse", "Claude API returned no text content.", isRetryable: false);
            }

            LlmExtractedFields? fields;
            try
            {
                fields = JsonSerializer.Deserialize<LlmExtractedFields>(rawText, JsonOptions);
            }
            catch (JsonException ex)
            {
                return new LlmExtractionResult
                {
                    IsSuccessful = false,
                    Raw = new LlmRawExtractionResult { RawJson = rawText },
                    Fields = null,
                    Metadata = BuildMetadata(),
                    Error = new ExtractionError { Code = "MalformedJson", Message = ex.Message, IsRetryable = false }
                };
            }

            return new LlmExtractionResult
            {
                IsSuccessful = true,
                Raw = new LlmRawExtractionResult { RawJson = rawText },
                Fields = fields,
                Metadata = BuildMetadata()
            };
        }
        catch (OperationCanceledException)
        {
            return Failure("Cancelled", "The extraction request was cancelled or timed out.", isRetryable: false);
        }
        catch (HttpRequestException ex)
        {
            return Failure("NetworkError", ex.Message, isRetryable: true);
        }
    }

    private ExtractionMetadata BuildMetadata() => new()
    {
        Model = _options.Model,
        ExtractedAtUtc = DateTime.UtcNow,
        Warnings = []
    };

    private LlmExtractionResult Failure(string code, string message, bool isRetryable) => new()
    {
        IsSuccessful = false,
        Raw = new LlmRawExtractionResult { RawJson = null },
        Fields = null,
        Metadata = BuildMetadata(),
        Error = new ExtractionError { Code = code, Message = message, IsRetryable = isRetryable }
    };

    private sealed class ClaudeRequestBody
    {
        public string Model { get; set; } = string.Empty;
        public int MaxTokens { get; set; }
        public string System { get; set; } = string.Empty;
        public List<ClaudeMessage> Messages { get; set; } = [];
    }

    private sealed class ClaudeMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private sealed class ClaudeResponseBody
    {
        public List<ClaudeContentBlock>? Content { get; set; }
        public string? Model { get; set; }
    }

    private sealed class ClaudeContentBlock
    {
        public string? Type { get; set; }
        public string? Text { get; set; }
    }
}
