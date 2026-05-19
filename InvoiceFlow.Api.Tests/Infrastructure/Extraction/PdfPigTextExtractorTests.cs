using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Infrastructure.Extraction;

namespace InvoiceFlow.Api.Tests.Infrastructure.Extraction;

public sealed class PdfPigTextExtractorTests
{
    [Fact]
    public async Task ExtractTextAsync_ShouldReturnNonEmptyText_ForTextBasedPdf()
    {
        var extractor = new PdfPigTextExtractor();
        var pdfPath = CreateMinimalTextPdf();

        try
        {
            var file = new FolderInvoiceFile
            {
                FileName = "invoice.pdf",
                FullPath = pdfPath,
                ContentType = "application/pdf"
            };

            var result = await extractor.ExtractTextAsync(file);

            Assert.False(string.IsNullOrWhiteSpace(result));
            Assert.Contains("test invoice", result, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(pdfPath);
        }
    }

    [Fact]
    public async Task ExtractTextAsync_ShouldReturnEmptyString_ForJpegContentType()
    {
        var extractor = new PdfPigTextExtractor();
        var file = new FolderInvoiceFile
        {
            FileName = "invoice.jpg",
            FullPath = Path.Combine(Path.GetTempPath(), "invoice.jpg"),
            ContentType = "image/jpeg"
        };

        var result = await extractor.ExtractTextAsync(file);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task ExtractTextAsync_ShouldReturnEmptyString_ForPngContentType()
    {
        var extractor = new PdfPigTextExtractor();
        var file = new FolderInvoiceFile
        {
            FileName = "invoice.png",
            FullPath = Path.Combine(Path.GetTempPath(), "invoice.png"),
            ContentType = "image/png"
        };

        var result = await extractor.ExtractTextAsync(file);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task ExtractTextAsync_ShouldReturnEmptyString_ForTiffContentType()
    {
        var extractor = new PdfPigTextExtractor();
        var file = new FolderInvoiceFile
        {
            FileName = "invoice.tiff",
            FullPath = Path.Combine(Path.GetTempPath(), "invoice.tiff"),
            ContentType = "image/tiff"
        };

        var result = await extractor.ExtractTextAsync(file);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task ExtractTextAsync_ShouldThrowArgumentNullException_WhenFileIsNull()
    {
        var extractor = new PdfPigTextExtractor();

        var act = async () => await extractor.ExtractTextAsync(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    /// <summary>
    /// Creates a minimal valid text-based PDF and returns the path to the temp file.
    /// The caller is responsible for deleting it.
    /// </summary>
    private static string CreateMinimalTextPdf()
    {
        // Carefully constructed minimal PDF with precomputed xref offsets.
        // Stream content "BT /F1 12 Tf 50 700 Td (test invoice) Tj ET\n" is 44 bytes.
        // Object offsets (0-indexed byte positions with \n line endings):
        //   obj1 =   9, obj2 =  52, obj3 = 102, obj4 = 212, obj5 = 303, xref = 364
        const string pdfContent =
            "%PDF-1.4\n" +
            "1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n" +
            "2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n" +
            "3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Contents 4 0 R/Resources<</Font<</F1 5 0 R>>>>>>endobj\n" +
            "4 0 obj\n" +
            "<</Length 44>>\n" +
            "stream\n" +
            "BT /F1 12 Tf 50 700 Td (test invoice) Tj ET\n" +
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
            "0000000303 00000 n \n" +
            "trailer<</Size 6/Root 1 0 R>>\n" +
            "startxref\n" +
            "364\n" +
            "%%EOF";

        var path = Path.Combine(Path.GetTempPath(), $"test-invoice-{Guid.NewGuid()}.pdf");
        File.WriteAllText(path, pdfContent, System.Text.Encoding.Latin1);

        return path;
    }
}
