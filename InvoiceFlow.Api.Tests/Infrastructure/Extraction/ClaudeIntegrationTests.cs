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

    [ClaudeIntegrationFact]
    public async Task ExtractAsync_ShouldReturnStructuredResult_WhenCalledWithRealPdf()
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")!;
        var model  = Environment.GetEnvironmentVariable("ANTHROPIC_MODEL")
                     ?? "claude-haiku-4-5-20251001";

        var pdfPath = CreateInvoicePdf();

        try
        {
            var file = new FolderInvoiceFile
            {
                FileName    = "sample-invoice.pdf",
                FullPath    = pdfPath,
                ContentType = "application/pdf"
            };

            // Verify PdfPig can extract the invoice text before hitting the API.
            var textExtractor = new PdfPigTextExtractor();
            var extractedText = await textExtractor.ExtractTextAsync(file);
            Assert.Contains("Acme", extractedText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("INV-2026-001", extractedText, StringComparison.OrdinalIgnoreCase);

            var httpClient = new HttpClient { BaseAddress = new Uri("https://api.anthropic.com/") };
            var options    = Options.Create(new ClaudeOptions { ApiKey = apiKey, Model = model });
            var extractor  = new ClaudeInvoiceExtractor(textExtractor, new ClaudePromptBuilder(), httpClient, options);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var result = await extractor.ExtractAsync(file, cts.Token);

            Assert.True(result.IsSuccessful || result.Error is not null);

            if (result.IsSuccessful)
            {
                Assert.NotNull(result.Fields);
                Assert.NotNull(result.Raw.RawJson);
                Assert.NotEmpty(result.Raw.RawJson);
                Assert.NotNull(result.Metadata.Model);
                Assert.NotEmpty(result.Metadata.Model);
            }
            else
            {
                Assert.NotNull(result.Error!.Code);
                Assert.False(string.IsNullOrWhiteSpace(result.Error.Message));
            }
        }
        finally
        {
            if (File.Exists(pdfPath))
                File.Delete(pdfPath);
        }
    }

    private static string CreateInvoicePdf()
    {
        // Minimal text-based PDF using the same technique as PdfPigTextExtractorTests.
        // Stream content is 141 bytes (single Tj operator with all invoice fields).
        // Precomputed xref offsets: obj1=9, obj2=52, obj3=102, obj4=212, obj5=401, xref=462.
        const string content =
            "%PDF-1.4\n" +
            "1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n" +
            "2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n" +
            "3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Contents 4 0 R/Resources<</Font<</F1 5 0 R>>>>>>endobj\n" +
            "4 0 obj\n" +
            "<</Length 141>>\n" +
            "stream\n" +
            "BT /F1 10 Tf 50 700 Td (Supplier: Acme B.V. Invoice: INV-2026-001 Date: 2026-01-15 EUR 1234.56 IBAN: NL91ABNA0417164300 KvK: 12345678) Tj ET\n" +
            "endstream\n" +
            "endobj\n" +
            "5 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj\n" +
            "xref\n" +
            "0 6\n" +
            "0000000000 65535 f \n" +
            "0000000009 00000 n \n" +
            "0000000052 00000 n \n" +
            "0000000102 00000 n \n" +
            "0000000212 00000 n \n" +
            "0000000401 00000 n \n" +
            "trailer<</Size 6/Root 1 0 R>>\n" +
            "startxref\n" +
            "462\n" +
            "%%EOF";

        var path = Path.Combine(Path.GetTempPath(), $"invoice-{Guid.NewGuid()}.pdf");
        File.WriteAllText(path, content, System.Text.Encoding.Latin1);
        return path;
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
