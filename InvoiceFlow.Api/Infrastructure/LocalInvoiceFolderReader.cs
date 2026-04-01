using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

namespace InvoiceFlow.Api.Infrastructure;

public sealed class LocalInvoiceFolderReader : IInvoiceFolderReader
{
    private static readonly string[] SupportedExtensions =
    {
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png"
    };

    public FolderInvoiceFile? GetNext(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return null;
        }

        if (!Directory.Exists(folderPath))
        {
            return null;
        }

        var filePath = Directory
            .GetFiles(folderPath)
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .OrderBy(path => path)
            .FirstOrDefault();

        if (filePath is null)
        {
            return null;
        }

        return new FolderInvoiceFile
        {
            FileName = Path.GetFileName(filePath),
            FullPath = filePath,
            ContentType = GetContentType(filePath)
        };
    }

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath);

        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }
}