using InvoiceFlow.Api.Features.Invoices.Extraction;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Infrastructure.Extraction;
using InvoiceFlow.Api.Tests.Fakes;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.Json;

namespace InvoiceFlow.Api.Tests.Infrastructure.Extraction;

public sealed class ClaudeInvoiceExtractorTests
{
    private static readonly FolderInvoiceFile AnyFile = new()
    {
        FileName = "invoice.pdf",
        FullPath = "C:\\invoices\\invoice.pdf",
        ContentType = "application/pdf"
    };

    [Fact]
    public async Task ExtractAsync_ShouldReturnSuccessResult_WhenClaudeReturnsValidJson()
    {
        var claudeJson = """
            {
              "supplier_name": "Acme BV",
              "invoice_number": "INV-001",
              "invoice_date": "2026-04-01",
              "total_amount": 123.45,
              "currency": "EUR"
            }
            """;
        var extractor = BuildExtractor(responseBody: WrapInClaudeEnvelope(claudeJson));

        var result = await extractor.ExtractAsync(AnyFile);

        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Fields);
        Assert.Equal("Acme BV", result.Fields!.SupplierName);
        Assert.Equal("INV-001", result.Fields.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 4, 1), result.Fields.InvoiceDate);
        Assert.Equal(123.45m, result.Fields.TotalAmount);
        Assert.Equal("EUR", result.Fields.Currency);
    }

    [Fact]
    public async Task ExtractAsync_ShouldPreserveRawJson_WhenClaudeReturnsValidJson()
    {
        var claudeJson = """{"supplier_name":"Acme BV","invoice_number":"INV-001"}""";
        var extractor = BuildExtractor(responseBody: WrapInClaudeEnvelope(claudeJson));

        var result = await extractor.ExtractAsync(AnyFile);

        Assert.NotNull(result.Raw.RawJson);
        Assert.Contains("supplier_name", result.Raw.RawJson);
    }

    [Fact]
    public async Task ExtractAsync_ShouldReturnFailureResult_WhenClaudeReturnsHttpError()
    {
        var extractor = BuildExtractor(statusCode: HttpStatusCode.InternalServerError);

        var result = await extractor.ExtractAsync(AnyFile);

        Assert.False(result.IsSuccessful);
        Assert.NotNull(result.Error);
        Assert.Equal("HttpError", result.Error!.Code);
    }

    [Fact]
    public async Task ExtractAsync_ShouldReturnFailureResult_WhenResponseIsNotValidJson()
    {
        var extractor = BuildExtractor(responseBody: WrapInClaudeEnvelope("this is not json at all"));

        var result = await extractor.ExtractAsync(AnyFile);

        Assert.False(result.IsSuccessful);
        Assert.NotNull(result.Error);
        Assert.Equal("MalformedJson", result.Error!.Code);
    }

    [Fact]
    public async Task ExtractAsync_ShouldReturnFailureResult_WhenRequestIsCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var extractor = BuildExtractor(responseBody: WrapInClaudeEnvelope("{}"));

        var result = await extractor.ExtractAsync(AnyFile, cts.Token);

        Assert.False(result.IsSuccessful);
        Assert.NotNull(result.Error);
        Assert.Equal("Cancelled", result.Error!.Code);
    }

    [Fact]
    public async Task ExtractAsync_ShouldReturnMissingApiKeyFailure_WhenApiKeyIsEmpty()
    {
        var extractor = BuildExtractor(apiKey: "");

        var result = await extractor.ExtractAsync(AnyFile);

        Assert.False(result.IsSuccessful);
        Assert.Equal("MissingApiKey", result.Error!.Code);
        Assert.False(result.Error.IsRetryable);
    }

    [Fact]
    public async Task ExtractAsync_ShouldReturnNetworkErrorFailure_WhenHttpRequestThrows()
    {
        var extractor = BuildExtractorWithThrowingHandler(new HttpRequestException("Connection refused."));

        var result = await extractor.ExtractAsync(AnyFile);

        Assert.False(result.IsSuccessful);
        Assert.Equal("NetworkError", result.Error!.Code);
        Assert.True(result.Error.IsRetryable);
    }

    [Theory]
    [InlineData(429, true)]
    [InlineData(500, true)]
    [InlineData(503, true)]
    [InlineData(400, false)]
    [InlineData(401, false)]
    [InlineData(403, false)]
    public async Task ExtractAsync_ShouldSetIsRetryable_BasedOnHttpStatusCode(int statusCode, bool expectedRetryable)
    {
        var extractor = BuildExtractor(statusCode: (HttpStatusCode)statusCode);

        var result = await extractor.ExtractAsync(AnyFile);

        Assert.False(result.IsSuccessful);
        Assert.Equal("HttpError", result.Error!.Code);
        Assert.Equal(expectedRetryable, result.Error.IsRetryable);
    }

    [Fact]
    public async Task ExtractAsync_ShouldSetIsRetryableToFalse_ForMalformedJson()
    {
        var extractor = BuildExtractor(responseBody: WrapInClaudeEnvelope("this is not json at all"));

        var result = await extractor.ExtractAsync(AnyFile);

        Assert.False(result.IsSuccessful);
        Assert.Equal("MalformedJson", result.Error!.Code);
        Assert.False(result.Error.IsRetryable);
    }

    [Fact]
    public async Task ExtractAsync_ShouldReturnEnvelopeParseError_WhenClaudeEnvelopeIsMalformed()
    {
        var extractor = BuildExtractor(responseBody: "this is not valid json");

        var result = await extractor.ExtractAsync(AnyFile);

        Assert.False(result.IsSuccessful);
        Assert.Equal("EnvelopeParseError", result.Error!.Code);
        Assert.False(result.Error.IsRetryable);
    }

    [Fact]
    public async Task ExtractAsync_ShouldRecordModelName_FromOptions()
    {
        var claudeJson = """{"supplier_name":"Test BV"}""";
        var extractor = BuildExtractor(responseBody: WrapInClaudeEnvelope(claudeJson), model: "claude-test-model");

        var result = await extractor.ExtractAsync(AnyFile);

        Assert.Equal("claude-test-model", result.Metadata.Model);
    }

    private static ClaudeInvoiceExtractor BuildExtractor(
        string? responseBody = null,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string model = "claude-test",
        string apiKey = "test-key")
    {
        var handler = new FakeHttpMessageHandler(statusCode, responseBody ?? "{}");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com/") };
        var options = Options.Create(new ClaudeOptions { ApiKey = apiKey, Model = model });
        return new ClaudeInvoiceExtractor(new FakeInvoiceTextExtractor(), new ClaudePromptBuilder(), httpClient, options);
    }

    private static ClaudeInvoiceExtractor BuildExtractorWithThrowingHandler(HttpRequestException exception)
    {
        var handler = new ThrowingHttpMessageHandler(exception);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com/") };
        var options = Options.Create(new ClaudeOptions { ApiKey = "test-key", Model = "claude-test" });
        return new ClaudeInvoiceExtractor(new FakeInvoiceTextExtractor(), new ClaudePromptBuilder(), httpClient, options);
    }

    private static string WrapInClaudeEnvelope(string content) =>
        $$"""
        {
          "id": "msg_test",
          "type": "message",
          "role": "assistant",
          "content": [{ "type": "text", "text": {{JsonSerializer.Serialize(content)}} }],
          "model": "claude-test",
          "stop_reason": "end_turn"
        }
        """;
}

file sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _responseBody;

    public FakeHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
    {
        _statusCode = statusCode;
        _responseBody = responseBody;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
        };

        return Task.FromResult(response);
    }
}

file sealed class ThrowingHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpRequestException _exception;

    public ThrowingHttpMessageHandler(HttpRequestException exception)
    {
        _exception = exception;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => throw _exception;
}
