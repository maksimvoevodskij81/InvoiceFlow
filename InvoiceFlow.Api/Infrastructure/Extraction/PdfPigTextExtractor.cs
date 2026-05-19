using InvoiceFlow.Api.Features.Invoices.Extraction;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using System.Text;
using UglyToad.PdfPig;

namespace InvoiceFlow.Api.Infrastructure.Extraction;

public sealed class PdfPigTextExtractor : IInvoiceTextExtractor
{
    public Task<string> ExtractTextAsync(FolderInvoiceFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (!IsPdf(file.ContentType))
        {
            return Task.FromResult(string.Empty);
        }

        return Task.FromResult(ExtractPdfText(file.FullPath));
    }

    private static bool IsPdf(string contentType)
    {
        return string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractPdfText(string fullPath)
    {
        using var document = PdfDocument.Open(fullPath);

        var builder = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            builder.AppendLine(page.Text);
        }

        return builder.ToString().Trim();
    }
}
