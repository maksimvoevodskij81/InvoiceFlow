using InvoiceFlow.Api.Features.Invoices.UploadInvoice;

namespace InvoiceFlow.Api.Infrastructure;

public sealed class LocalUploadedInvoiceFileStore : IUploadedInvoiceFileStore
{
    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        var uploadsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "UploadedInvoices");

        Directory.CreateDirectory(uploadsFolderPath);

        var fileExtension = Path.GetExtension(file.FileName);
        var storedFileName = $"{Guid.NewGuid()}{fileExtension}";
        var fullPath = Path.Combine(uploadsFolderPath, storedFileName);

        await using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);

        await file.CopyToAsync(stream, cancellationToken);

        return fullPath;
    }
}