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
        string model = "claude-test")
    {
        var handler = new FakeHttpMessageHandler(statusCode, responseBody ?? "{}");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com/") };
        var options = Options.Create(new ClaudeOptions { ApiKey = "test-key", Model = model });
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
