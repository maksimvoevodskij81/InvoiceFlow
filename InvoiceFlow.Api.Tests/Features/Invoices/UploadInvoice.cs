using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using InvoiceFlow.Api.Infrastructure;
using InvoiceFlow.Api.Tests.Fakes;
using Microsoft.AspNetCore.Http;

namespace InvoiceFlow.Api.Tests.Features.Invoices.UploadInvoice;

[Collection("NonParallel File System Tests")]
public sealed class InvoiceUploadServiceTests
{
    [Fact]
    public async Task UploadAsync_ShouldSaveRecord_ParseAndReturnParsed_WhenParsingSucceeds()
    {
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempRoot);
        Directory.SetCurrentDirectory(tempRoot);

        try
        {
            var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
            var service = new InvoiceUploadService(
                new FakeInvoiceParser(),
                new LocalUploadedInvoiceFileStore(),
                uploadedInvoiceStore);

            await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "invoice.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var response = await service.UploadAsync(file, CancellationToken.None);

            Assert.False(string.IsNullOrWhiteSpace(response.InvoiceId));
            Assert.Equal(InvoiceStatuses.Parsed, response.Status);
            Assert.Equal("Invoice parsed successfully.", response.Message);

            var savedRecord = await uploadedInvoiceStore.GetByIdAsync(response.InvoiceId, CancellationToken.None);

            Assert.NotNull(savedRecord);
            Assert.Equal("invoice.pdf", savedRecord.OriginalFileName);
            Assert.False(string.IsNullOrWhiteSpace(savedRecord.StoredFilePath));
            Assert.EndsWith(".pdf", savedRecord.StoredFilePath, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("UploadedInvoices", savedRecord.StoredFilePath, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(savedRecord.StoredFilePath));
            Assert.Equal(InvoiceStatuses.Parsed, savedRecord.Status);
            Assert.Equal("Invoice parsed successfully.", savedRecord.Message);
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
    public async Task UploadAsync_ShouldUpdateStatusToFailed_AndReturnFailed_WhenParsingThrows()
    {
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempRoot);
        Directory.SetCurrentDirectory(tempRoot);

        try
        {
            var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
            var service = new InvoiceUploadService(
                new ThrowingInvoiceParser(),
                new LocalUploadedInvoiceFileStore(),
                uploadedInvoiceStore);

            await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "invoice.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var response = await service.UploadAsync(file, CancellationToken.None);

            Assert.False(string.IsNullOrWhiteSpace(response.InvoiceId));
            Assert.Equal(InvoiceStatuses.Failed, response.Status);
            Assert.Equal("Invoice parsing failed.", response.Message);

            var savedRecord = await uploadedInvoiceStore.GetByIdAsync(response.InvoiceId, CancellationToken.None);

            Assert.NotNull(savedRecord);
            Assert.Equal("invoice.pdf", savedRecord.OriginalFileName);
            Assert.False(string.IsNullOrWhiteSpace(savedRecord.StoredFilePath));
            Assert.EndsWith(".pdf", savedRecord.StoredFilePath, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("UploadedInvoices", savedRecord.StoredFilePath, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(savedRecord.StoredFilePath));
            Assert.Equal(InvoiceStatuses.Failed, savedRecord.Status);
            Assert.Equal("Invoice parsing failed.", savedRecord.Message);
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

    private sealed class ThrowingInvoiceParser : IInvoiceParser
    {
        public Task<InvoiceParseResult> ParseAsync(FolderInvoiceFile file, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Parsing failed.");
        }
    }
}