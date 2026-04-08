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
    public async Task UploadAsync_ShouldSaveRecord_ParseMatchAndReturnParsed_WhenSupplierIsMatched()
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
                new FakeSupplierMatcher(),
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
            Assert.False(string.IsNullOrWhiteSpace(savedRecord.FileHash));
            Assert.Equal(InvoiceStatuses.Parsed, savedRecord.Status);
            Assert.Equal("Invoice parsed successfully.", savedRecord.Message);
            Assert.Equal("Demo Supplier", savedRecord.SupplierName);
            Assert.Equal("INV-001", savedRecord.InvoiceNumber);
            Assert.Equal(new DateOnly(2026, 4, 1), savedRecord.InvoiceDate);
            Assert.Equal(123.45m, savedRecord.TotalAmount);
            Assert.Equal("EUR", savedRecord.Currency);
            Assert.True(savedRecord.IsSupplierMatched);
            Assert.False(savedRecord.RequiresSupplierReview);
            Assert.Equal("BankAccount", savedRecord.SupplierMatchedBy);
            Assert.Equal("internal-supplier-001", savedRecord.InternalSupplierId);
            Assert.Equal("exact-supplier-001", savedRecord.ExactSupplierId);
            Assert.Equal("Supplier matched successfully.", savedRecord.SupplierMatchMessage);
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
    public async Task UploadAsync_ShouldReturnDuplicate_WhenFileWithSameHashAlreadyExists()
    {
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempRoot);
        Directory.SetCurrentDirectory(tempRoot);

        try
        {
            var parser = new CountingInvoiceParser();
            var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
            var service = new InvoiceUploadService(
                parser,
                new FakeSupplierMatcher(),
                new LocalUploadedInvoiceFileStore(),
                uploadedInvoiceStore);

            var firstFile = CreatePdfFormFile(new byte[] { 1, 2, 3 }, "invoice.pdf");
            var secondFile = CreatePdfFormFile(new byte[] { 1, 2, 3 }, "invoice-copy.pdf");

            var firstResponse = await service.UploadAsync(firstFile, CancellationToken.None);
            var duplicateResponse = await service.UploadAsync(secondFile, CancellationToken.None);

            Assert.False(string.IsNullOrWhiteSpace(firstResponse.InvoiceId));
            Assert.Equal(InvoiceStatuses.Parsed, firstResponse.Status);

            Assert.Equal(firstResponse.InvoiceId, duplicateResponse.InvoiceId);
            Assert.Equal(InvoiceStatuses.Duplicate, duplicateResponse.Status);
            Assert.Equal("Duplicate invoice upload detected.", duplicateResponse.Message);
            Assert.Equal(1, parser.CallCount);
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
    public async Task UploadAsync_ShouldSaveReviewFlags_WhenSupplierNeedsReview()
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
                new ReviewRequiredSupplierMatcher(),
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
            Assert.False(string.IsNullOrWhiteSpace(savedRecord.StoredFilePath));
            Assert.EndsWith(".pdf", savedRecord.StoredFilePath, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("UploadedInvoices", savedRecord.StoredFilePath, StringComparison.OrdinalIgnoreCase);
            Assert.False(savedRecord.IsSupplierMatched);
            Assert.True(savedRecord.RequiresSupplierReview);
            Assert.Equal("Name", savedRecord.SupplierMatchedBy);
            Assert.Null(savedRecord.InternalSupplierId);
            Assert.Null(savedRecord.ExactSupplierId);
            Assert.Equal("Multiple supplier candidates found. Review required.", savedRecord.SupplierMatchMessage);
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
                new FakeSupplierMatcher(),
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
            Assert.False(string.IsNullOrWhiteSpace(savedRecord.FileHash));
            Assert.Equal(InvoiceStatuses.Failed, savedRecord.Status);
            Assert.Equal("Invoice parsing failed.", savedRecord.Message);
            Assert.Null(savedRecord.SupplierName);
            Assert.Null(savedRecord.InvoiceNumber);
            Assert.Null(savedRecord.InvoiceDate);
            Assert.Null(savedRecord.TotalAmount);
            Assert.Null(savedRecord.Currency);
            Assert.False(savedRecord.IsSupplierMatched);
            Assert.False(savedRecord.RequiresSupplierReview);
            Assert.Null(savedRecord.SupplierMatchedBy);
            Assert.Null(savedRecord.InternalSupplierId);
            Assert.Null(savedRecord.ExactSupplierId);
            Assert.Null(savedRecord.SupplierMatchMessage);
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

    private static IFormFile CreatePdfFormFile(byte[] bytes, string fileName)
    {
        var stream = new MemoryStream(bytes);

        return new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
    }

    private sealed class CountingInvoiceParser : IInvoiceParser
    {
        public int CallCount { get; private set; }

        public Task<InvoiceParseResult> ParseAsync(FolderInvoiceFile file, CancellationToken cancellationToken = default)
        {
            CallCount++;

            var result = new InvoiceParseResult
            {
                SupplierName = "Demo Supplier",
                InvoiceNumber = "INV-001",
                InvoiceDate = new DateOnly(2026, 4, 1),
                TotalAmount = 123.45m,
                Currency = "EUR"
            };

            return Task.FromResult(result);
        }
    }

    private sealed class ThrowingInvoiceParser : IInvoiceParser
    {
        public Task<InvoiceParseResult> ParseAsync(FolderInvoiceFile file, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Parsing failed.");
        }
    }

    private sealed class ReviewRequiredSupplierMatcher : ISupplierMatcher
    {
        public Task<SupplierMatchResult> MatchAsync(InvoiceParseResult parseResult, CancellationToken cancellationToken = default)
        {
            var result = new SupplierMatchResult
            {
                IsMatched = false,
                RequiresReview = true,
                MatchedBy = "Name",
                InternalSupplierId = null,
                ExactSupplierId = null,
                Message = "Multiple supplier candidates found. Review required."
            };

            return Task.FromResult(result);
        }
    }
}