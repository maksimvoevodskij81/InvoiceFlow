using InvoiceFlow.Api.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace InvoiceFlow.Api.Tests.Infrastructure;

[Collection("NonParallel File System Tests")]
public sealed class LocalUploadedInvoiceFileStoreTests
{
    [Fact]
    public async Task SaveAsync_ShouldSaveFileToUploadedInvoicesFolder_AndReturnFullPath()
    {
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempRoot);
        Directory.SetCurrentDirectory(tempRoot);

        try
        {
            var fileStore = new LocalUploadedInvoiceFileStore();
            await using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "invoice.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var savedPath = await fileStore.SaveAsync(file);

            Assert.NotNull(savedPath);
            Assert.True(File.Exists(savedPath));
            Assert.Equal(".pdf", Path.GetExtension(savedPath));

            var uploadsFolderPath = Path.Combine(tempRoot, "UploadedInvoices");
            Assert.StartsWith(uploadsFolderPath, savedPath, StringComparison.OrdinalIgnoreCase);

            var savedBytes = await File.ReadAllBytesAsync(savedPath);
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, savedBytes);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SaveAsync_ShouldCreateUploadedInvoicesFolder_WhenItDoesNotExist()
    {
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempRoot);
        Directory.SetCurrentDirectory(tempRoot);

        try
        {
            var fileStore = new LocalUploadedInvoiceFileStore();
            await using var stream = new MemoryStream(new byte[] { 9, 8, 7 });
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "invoice.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            var uploadsFolderPath = Path.Combine(tempRoot, "UploadedInvoices");

            Assert.False(Directory.Exists(uploadsFolderPath));

            var savedPath = await fileStore.SaveAsync(file);

            Assert.True(Directory.Exists(uploadsFolderPath));
            Assert.True(File.Exists(savedPath));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SaveAsync_ShouldThrowArgumentNullException_WhenFileIsNull()
    {
        var fileStore = new LocalUploadedInvoiceFileStore();

        var act = async () => await fileStore.SaveAsync(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }
}