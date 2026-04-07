using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Controllers;
using InvoiceFlow.Api.Features.Invoices.GetInvoiceStatus;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using InvoiceFlow.Api.Infrastructure;
using Microsoft.AspNetCore.Http;
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
            new FakeSupplierMatcher(),
            new LocalUploadedInvoiceFileStore(),
            new FakeUploadedInvoiceStore());

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
            new FakeSupplierMatcher(),
            new LocalUploadedInvoiceFileStore()
            , new FakeUploadedInvoiceStore());

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

    [Fact]
    public async Task Upload_ShouldReturnBadRequest_WhenFileIsMissing()
    {
        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher(),
            new LocalUploadedInvoiceFileStore()
            , new FakeUploadedInvoiceStore());

        var request = new UploadInvoiceRequest
        {
            File = default!
        };

        var result = await controller.Upload(request, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal("File is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task Upload_ShouldReturnBadRequest_WhenFileTypeIsNotSupported()
    {
        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher(),
            new LocalUploadedInvoiceFileStore(),
            new FakeUploadedInvoiceStore());

        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var formFile = new FormFile(stream, 0, stream.Length, "file", "invoice.docx");

        var request = new UploadInvoiceRequest
        {
            File = formFile
        };

        var result = await controller.Upload(request, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal("Only PDF, JPG, JPEG, PNG, TIF, and TIFF files are allowed.", badRequestResult.Value);
    }

    [Fact]
    public async Task Upload_ShouldReturnBadRequest_WhenFileIsTooLarge()
    {
        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher(),
            new LocalUploadedInvoiceFileStore(),
            new FakeUploadedInvoiceStore());

        var buffer = new byte[(10 * 1024 * 1024) + 1];
        await using var stream = new MemoryStream(buffer);

        var formFile = new FormFile(stream, 0, stream.Length, "file", "invoice.pdf");

        var request = new UploadInvoiceRequest
        {
            File = formFile
        };

        var result = await controller.Upload(request, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal("File size must not exceed 10 MB.", badRequestResult.Value);
    }

    [Fact]
    public async Task GetStatus_ShouldReturnProcessingResponse()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();

        await uploadedInvoiceStore.SaveAsync(new UploadedInvoiceRecord
        {
            InvoiceId = "123",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = Path.Combine("temp", "invoice.pdf"),
            Status = InvoiceStatuses.Processing,
            Message = "Invoice is still being processed.",
            CreatedAtUtc = DateTime.UtcNow
        });

        var controller = new InvoicesController(
               new LocalInvoiceFolderReader(),
               new FakeInvoiceParser(),
               new FakeSupplierMatcher(),
               new LocalUploadedInvoiceFileStore(),
               uploadedInvoiceStore);

        var result = await controller.GetStatus("123", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<GetInvoiceStatusResponse>(okResult.Value);

        Assert.Equal("123", response.InvoiceId);
        Assert.Equal(InvoiceStatuses.Processing, response.Status);
        Assert.Equal("Invoice is still being processed.", response.Message);
    }

    [Fact]
    public async Task Upload_ShouldSaveUploadedInvoiceRecord_AndReturnProcessingResponse()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher(),
            new LocalUploadedInvoiceFileStore(),
            uploadedInvoiceStore);

        await using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var formFile = new FormFile(stream, 0, stream.Length, "file", "invoice.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var request = new UploadInvoiceRequest
        {
            File = formFile
        };

        var result = await controller.Upload(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<UploadInvoiceAcceptedResponse>(okResult.Value);

        Assert.False(string.IsNullOrWhiteSpace(response.InvoiceId));
        Assert.Equal(InvoiceStatuses.Processing, response.Status);
        Assert.Equal("Invoice upload received.", response.Message);

        var savedRecord = await uploadedInvoiceStore.GetByIdAsync(response.InvoiceId, CancellationToken.None);

        Assert.NotNull(savedRecord);
        Assert.Equal(response.InvoiceId, savedRecord.InvoiceId);
        Assert.Equal("invoice.pdf", savedRecord.OriginalFileName);
        Assert.Equal(InvoiceStatuses.Processing, savedRecord.Status);
        Assert.Equal("Invoice upload received.", savedRecord.Message);
        Assert.False(string.IsNullOrWhiteSpace(savedRecord.StoredFilePath));
    }

    [Fact]
    public async Task GetStatus_ShouldReturnNotFound_WhenInvoiceDoesNotExist()
    {
        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher(),
            new LocalUploadedInvoiceFileStore(),
            new FakeUploadedInvoiceStore());

        var result = await controller.GetStatus("missing-id", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}