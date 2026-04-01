using InvoiceFlow.Api.Infrastructure;

namespace InvoiceFlow.Api.Tests.Infrastructure;

public sealed class LocalInvoiceFolderReaderTests
{
    [Fact]
    public void GetNext_ShouldReturnNull_WhenFolderDoesNotExist()
    {
        var reader = new LocalInvoiceFolderReader();

        var result = reader.GetNext(@"C:\definitely-not-existing-folder-123");

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
}