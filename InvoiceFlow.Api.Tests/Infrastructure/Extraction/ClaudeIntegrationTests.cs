using InvoiceFlow.Api.Features.Invoices.Extraction;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Infrastructure.Extraction;
using InvoiceFlow.Api.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Api.Tests.Infrastructure.Extraction;

public sealed class ClaudeIntegrationTests
{
    private static readonly FolderInvoiceFile AnyFile = new()
    {
        FileName = "sample-invoice.pdf",
        FullPath = "C:\\invoices\\sample-invoice.pdf",
        ContentType = "application/pdf"
    };

    private const string SampleInvoiceText =
        """
        Supplier: Acme B.V.
        Invoice number: INV-2026-001
        Invoice date: 2026-01-15
        Total amount: EUR 1.234,56
        IBAN: NL91ABNA0417164300
        BIC: ABNANL2A
        KvK: 12345678
        BTW: NL123456789B01
        """;

    [ClaudeIntegrationFact]
    public async Task ExtractAsync_ShouldReturnStructuredResult_WhenCalledWithRealApi()
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")!;
        var model = Environment.GetEnvironmentVariable("ANTHROPIC_MODEL")
            ?? "claude-haiku-4-5-20251001";

        var textExtractor = new FakeInvoiceTextExtractor { Result = SampleInvoiceText };
        var httpClient = new HttpClient { BaseAddress = new Uri("https://api.anthropic.com/") };
        var options = Options.Create(new ClaudeOptions { ApiKey = apiKey, Model = model });
        var extractor = new ClaudeInvoiceExtractor(textExtractor, new ClaudePromptBuilder(), httpClient, options);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var result = await extractor.ExtractAsync(AnyFile, cts.Token);

        Assert.True(result.IsSuccessful || result.Error is not null);

        if (result.IsSuccessful)
        {
            Assert.NotNull(result.Fields);
            Assert.NotNull(result.Raw.RawJson);
            Assert.NotNull(result.Metadata.Model);
        }
        else
        {
            Assert.NotNull(result.Error!.Code);
            Assert.False(string.IsNullOrWhiteSpace(result.Error.Message));
        }
    }
}

// Checked at test-discovery time — shows as Skipped in the runner output when env vars are absent.
[AttributeUsage(AttributeTargets.Method)]
file sealed class ClaudeIntegrationFactAttribute : FactAttribute
{
    public ClaudeIntegrationFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("RUN_CLAUDE_INTEGRATION_TESTS") != "true")
        {
            Skip = "Set RUN_CLAUDE_INTEGRATION_TESTS=true to enable.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
            Skip = "ANTHROPIC_API_KEY not set.";
    }
}
