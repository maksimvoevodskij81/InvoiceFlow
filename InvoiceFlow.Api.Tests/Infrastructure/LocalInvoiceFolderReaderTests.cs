using InvoiceFlow.Api.Infrastructure;

namespace InvoiceFlow.Api.Tests.Infrastructure;

[Collection("NonParallel File System Tests")]
public sealed class LocalInvoiceFolderReaderTests
{
    [Fact]
    public void GetNext_ShouldReturnNull_WhenFolderDoesNotExist()
    {
        var reader = new LocalInvoiceFolderReader();
        var folderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var result = reader.GetNext(folderPath);

        Assert.Null(result);
    }

    [Fact]
    public void GetNext_ShouldReturnNull_WhenFolderHasNoSupportedFiles()
    {
        var reader = new LocalInvoiceFolderReader();
        var folderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(folderPath);
        File.WriteAllText(Path.Combine(folderPath, "notes.txt"), "test");

        try
        {
            var result = reader.GetNext(folderPath);

            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(folderPath, true);
        }
    }

    [Fact]
    public void GetNext_ShouldReturnFirstSupportedFile_WhenFolderContainsSupportedFiles()
    {
        var reader = new LocalInvoiceFolderReader();
        var folderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(folderPath);
        File.WriteAllText(Path.Combine(folderPath, "b-invoice.pdf"), "test");
        File.WriteAllText(Path.Combine(folderPath, "a-image.png"), "test");

        try
        {
            var result = reader.GetNext(folderPath);

            Assert.NotNull(result);
            Assert.Equal("a-image.png", result.FileName);
            Assert.Equal(Path.Combine(folderPath, "a-image.png"), result.FullPath);
            Assert.Equal("image/png", result.ContentType);
        }
        finally
        {
            Directory.Delete(folderPath, true);
        }
    }

    [Fact]
    public void GetNext_ShouldIgnoreUnsupportedFiles_WhenFolderContainsMixedFiles()
    {
        var reader = new LocalInvoiceFolderReader();
        var folderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(folderPath);
        File.WriteAllText(Path.Combine(folderPath, "a-notes.txt"), "test");
        File.WriteAllText(Path.Combine(folderPath, "b-data.json"), "test");
        File.WriteAllText(Path.Combine(folderPath, "c-invoice.pdf"), "test");

        try
        {
            var result = reader.GetNext(folderPath);

            Assert.NotNull(result);
            Assert.Equal("c-invoice.pdf", result.FileName);
            Assert.Equal(Path.Combine(folderPath, "c-invoice.pdf"), result.FullPath);
            Assert.Equal("application/pdf", result.ContentType);
        }
        finally
        {
            Directory.Delete(folderPath, true);
        }
    }

    [Fact]
    public void TakeNext_ShouldMoveFileToProcessedFolder_WhenSupportedFileExists()
    {
        var reader = new LocalInvoiceFolderReader();
        var folderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(folderPath);
        File.WriteAllText(Path.Combine(folderPath, "invoice.pdf"), "test");

        try
        {
            var result = reader.TakeNext(folderPath);

            Assert.NotNull(result);
            Assert.Equal("invoice.pdf", result.FileName);
            Assert.Equal("application/pdf", result.ContentType);
            Assert.Equal(Path.Combine(folderPath, "Processed", "invoice.pdf"), result.FullPath);
            Assert.True(File.Exists(result.FullPath));
            Assert.False(File.Exists(Path.Combine(folderPath, "invoice.pdf")));
        }
        finally
        {
            Directory.Delete(folderPath, true);
        }
    }

    [Fact]
    public void TakeNext_ShouldReturnNull_WhenFolderHasNoSupportedFiles()
    {
        var reader = new LocalInvoiceFolderReader();
        var folderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(folderPath);
        File.WriteAllText(Path.Combine(folderPath, "notes.txt"), "test");

        try
        {
            var result = reader.TakeNext(folderPath);

            Assert.Null(result);
            Assert.False(Directory.Exists(Path.Combine(folderPath, "Processed")));
        }
        finally
        {
            Directory.Delete(folderPath, true);
        }
    }
}