using InvoiceFlow.Api.Controllers;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Api.Tests.Controllers;

public sealed class InvoicesControllerTests
{
    [Fact]
    public async Task ImportFromFolder_ShouldReturnBadRequest_WhenFolderPathIsEmpty()
    {
        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher());

        var request = new ImportInvoicesFromFolderRequest
        {
            FolderPath = string.Empty
        };

        var result = await controller.ImportFromFolder(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal("FolderPath is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task ImportFromFolder_ShouldReturnNotFound_WhenFolderHasNoSupportedFiles()
    {
        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher());

        var folderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(folderPath);
        File.WriteAllText(Path.Combine(folderPath, "notes.txt"), "test");

        try
        {
            var request = new ImportInvoicesFromFolderRequest
            {
                FolderPath = folderPath
            };

            var result = await controller.ImportFromFolder(request);

            Assert.IsType<NotFoundResult>(result.Result);
        }
        finally
        {
            Directory.Delete(folderPath, true);
        }
    }
}