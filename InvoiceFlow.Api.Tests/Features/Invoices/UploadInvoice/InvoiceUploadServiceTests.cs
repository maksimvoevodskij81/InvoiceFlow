using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Invoices;
using InvoiceFlow.Api.Features.Invoices.Extraction;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using InvoiceFlow.Api.Features.Suppliers.Matching;
using InvoiceFlow.Api.Infrastructure;
using InvoiceFlow.Api.Tests.Fakes;
using Microsoft.AspNetCore.Http;

namespace InvoiceFlow.Api.Tests.Features.Invoices.UploadInvoice;

[Collection("NonParallel File System Tests")]
public sealed class InvoiceUploadServiceTests
{
    [Fact]
    public async Task UploadAsync_ShouldSaveRecord_AndReturnReadyToPost_WhenSupplierIsMatchedWithoutReview()
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
                 uploadedInvoiceStore,
                 new FakeExactPostOutboxWriter(),
                 new InvoiceParseResultValidator(),
                 new SupplierCreateValidator(),
                 new FakeSupplierCreateOutboxWriter(),
                 new FakeBankDetailsRiskEvaluator());


            await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "invoice.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var response = await service.UploadAsync(file, cancellationToken: CancellationToken.None);

            Assert.False(string.IsNullOrWhiteSpace(response.InvoiceId));
            Assert.Equal(InvoiceStatuses.ReadyToPost, response.Status);
            Assert.Equal(InvoiceMessages.ReadyToPost, response.Message);

            var savedRecord = await uploadedInvoiceStore.GetByIdAsync(response.InvoiceId, CancellationToken.None);

            Assert.NotNull(savedRecord);
            Assert.Equal("invoice.pdf", savedRecord.OriginalFileName);
            Assert.False(string.IsNullOrWhiteSpace(savedRecord.StoredFilePath));
            Assert.EndsWith(".pdf", savedRecord.StoredFilePath, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("UploadedInvoices", savedRecord.StoredFilePath, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(savedRecord.StoredFilePath));
            Assert.False(string.IsNullOrWhiteSpace(savedRecord.FileHash));
            Assert.Equal(InvoiceStatuses.ReadyToPost, savedRecord.Status);
            Assert.Equal(InvoiceMessages.ReadyToPost, savedRecord.Message);
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
    public async Task UploadAsync_ShouldSetExtractionMetadata_WhenParsingSucceeds()
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
                 uploadedInvoiceStore,
                 new FakeExactPostOutboxWriter(),
                 new InvoiceParseResultValidator(),
                 new SupplierCreateValidator(),
                 new FakeSupplierCreateOutboxWriter(),
                 new FakeBankDetailsRiskEvaluator());

            await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "invoice.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var response = await service.UploadAsync(file, cancellationToken: CancellationToken.None);
            var savedRecord = await uploadedInvoiceStore.GetByIdAsync(response.InvoiceId, CancellationToken.None);

            Assert.NotNull(savedRecord);
            Assert.Equal(nameof(FakeInvoiceParser), savedRecord.ExtractionModel);
            Assert.NotNull(savedRecord.ExtractionCompletedAtUtc);
            Assert.Null(savedRecord.RawExtractionJson);
            Assert.Empty(savedRecord.ExtractionWarnings);
            Assert.Null(savedRecord.ExtractionError);
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
    public async Task UploadAsync_ShouldPopulateLlmExtractionMetadata_WhenLlmParserIsUsed()
    {
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempRoot);
        Directory.SetCurrentDirectory(tempRoot);

        try
        {
            var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
            var service = new InvoiceUploadService(
                new LlmInvoiceParser(new DemoLlmInvoiceExtractor()),
                new FakeSupplierMatcher(),
                new LocalUploadedInvoiceFileStore(),
                uploadedInvoiceStore,
                new FakeExactPostOutboxWriter(),
                new InvoiceParseResultValidator(),
                new SupplierCreateValidator(),
                new FakeSupplierCreateOutboxWriter(),
                new FakeBankDetailsRiskEvaluator());

            await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "invoice.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var response = await service.UploadAsync(file, cancellationToken: CancellationToken.None);
            var savedRecord = await uploadedInvoiceStore.GetByIdAsync(response.InvoiceId, CancellationToken.None);

            Assert.NotNull(savedRecord);
            Assert.Equal("demo", savedRecord.ExtractionModel);
            Assert.NotNull(savedRecord.ExtractionCompletedAtUtc);
            Assert.NotNull(savedRecord.RawExtractionJson);
            Assert.Empty(savedRecord.ExtractionWarnings);
            Assert.Null(savedRecord.ExtractionError);
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
                 uploadedInvoiceStore,
                 new FakeExactPostOutboxWriter(),
                 new InvoiceParseResultValidator(),
                 new SupplierCreateValidator(),
                 new FakeSupplierCreateOutboxWriter(),
                 new FakeBankDetailsRiskEvaluator());


            var firstFile = CreatePdfFormFile(new byte[] { 1, 2, 3 }, "invoice.pdf");
            var secondFile = CreatePdfFormFile(new byte[] { 1, 2, 3 }, "invoice-copy.pdf");

            var firstResponse = await service.UploadAsync(firstFile, cancellationToken: CancellationToken.None);
            var duplicateResponse = await service.UploadAsync(secondFile, cancellationToken: CancellationToken.None);

            Assert.False(string.IsNullOrWhiteSpace(firstResponse.InvoiceId));
            Assert.Equal(InvoiceStatuses.ReadyToPost, firstResponse.Status);

            Assert.Equal(firstResponse.InvoiceId, duplicateResponse.InvoiceId);
            Assert.Equal(InvoiceStatuses.Duplicate, duplicateResponse.Status);
            Assert.Equal(InvoiceMessages.DuplicateUploadDetected, duplicateResponse.Message);
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
            var parser = new CountingInvoiceParser();

            var service = new InvoiceUploadService(
                parser,
                new ReviewRequiredSupplierMatcher(),
                new LocalUploadedInvoiceFileStore(),
                uploadedInvoiceStore,
                new FakeExactPostOutboxWriter(),
                new InvoiceParseResultValidator(),
                new SupplierCreateValidator(),
                new FakeSupplierCreateOutboxWriter(),
                new FakeBankDetailsRiskEvaluator());

            await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "invoice.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var response = await service.UploadAsync(file, cancellationToken: CancellationToken.None);

            Assert.False(string.IsNullOrWhiteSpace(response.InvoiceId));
            Assert.Equal(InvoiceStatuses.NeedsReview, response.Status);
            Assert.Equal(InvoiceMessages.NeedsReview, response.Message);

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
                uploadedInvoiceStore,
                new FakeExactPostOutboxWriter(),
                new InvoiceParseResultValidator(),
                new SupplierCreateValidator(),
                new FakeSupplierCreateOutboxWriter(),
                new FakeBankDetailsRiskEvaluator());

            await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "invoice.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var response = await service.UploadAsync(file, cancellationToken: CancellationToken.None);

            Assert.False(string.IsNullOrWhiteSpace(response.InvoiceId));
            Assert.Equal(InvoiceStatuses.Failed, response.Status);
            Assert.Equal(InvoiceMessages.ParsingFailed, response.Message);

            var savedRecord = await uploadedInvoiceStore.GetByIdAsync(response.InvoiceId, CancellationToken.None);

            Assert.NotNull(savedRecord);
            Assert.Equal("invoice.pdf", savedRecord.OriginalFileName);
            Assert.False(string.IsNullOrWhiteSpace(savedRecord.StoredFilePath));
            Assert.EndsWith(".pdf", savedRecord.StoredFilePath, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("UploadedInvoices", savedRecord.StoredFilePath, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(savedRecord.StoredFilePath));
            Assert.False(string.IsNullOrWhiteSpace(savedRecord.FileHash));
            Assert.Equal(InvoiceStatuses.Failed, savedRecord.Status);
            Assert.Equal(InvoiceMessages.ParsingFailed, savedRecord.Message);
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
            Assert.Equal(nameof(ThrowingInvoiceParser), savedRecord.ExtractionModel);
            Assert.NotNull(savedRecord.ExtractionCompletedAtUtc);
            Assert.Null(savedRecord.RawExtractionJson);
            Assert.Empty(savedRecord.ExtractionWarnings);
            Assert.Equal(InvoiceMessages.ParsingFailed, savedRecord.ExtractionError);
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
    public async Task UploadAsync_ShouldEnqueueExactOutbox_WhenSupplierIsMatchedAndExactSupplierIdExists()
    {
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempRoot);
        Directory.SetCurrentDirectory(tempRoot);

        try
        {
            var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
            var outboxWriter = new FakeExactPostOutboxWriter();
            var service = new InvoiceUploadService(
                new FakeInvoiceParser(),
                new FakeSupplierMatcher(),
                new LocalUploadedInvoiceFileStore(),
                uploadedInvoiceStore,
                outboxWriter,
                new InvoiceParseResultValidator(),
                new SupplierCreateValidator(),
                new FakeSupplierCreateOutboxWriter(),
                new FakeBankDetailsRiskEvaluator());

            await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "invoice.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var response = await service.UploadAsync(file, cancellationToken: CancellationToken.None);

            Assert.Equal(InvoiceStatuses.ReadyToPost, response.Status);
            Assert.Equal(1, outboxWriter.EnqueueCallsCount);
            Assert.Equal(response.InvoiceId, outboxWriter.LastEnqueuedInvoiceId);
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
    public async Task UploadAsync_ShouldReturnMissingFields_WhenInvoiceIsInvalid()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();

        var service = new InvoiceUploadService(
            new InvalidInvoiceParser(), // ← important
            new FakeSupplierMatcher(),
            new LocalUploadedInvoiceFileStore(),
            uploadedInvoiceStore,
            new FakeExactPostOutboxWriter(),
            new InvoiceParseResultValidator(),
             new SupplierCreateValidator(),
             new FakeSupplierCreateOutboxWriter(),
             new FakeBankDetailsRiskEvaluator());

        IFormFile file = CreatePdfFormFile(new byte[] { 1, 2, 3 }, "invoice.pdf");

        var response = await service.UploadAsync(file, cancellationToken: CancellationToken.None);

        Assert.Equal(InvoiceStatuses.Invalid, response.Status);
        Assert.NotEmpty(response.MissingFields);

        Assert.Contains(nameof(InvoiceParseResult.SupplierName), response.MissingFields);
        Assert.Contains(nameof(InvoiceParseResult.InvoiceNumber), response.MissingFields);
    }

    [Fact]
    public async Task UploadAsync_ShouldSetCanCreateSupplierTrue_WhenNotMatchedAndAllSupplierFieldsPresent()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();

        var service = new InvoiceUploadService(
            new FullSupplierDataParser(), 
            new NotMatchedSupplierMatcher(),
            new LocalUploadedInvoiceFileStore(),
            uploadedInvoiceStore,
            new FakeExactPostOutboxWriter(),
            new InvoiceParseResultValidator(),
            new SupplierCreateValidator(), 
            new FakeSupplierCreateOutboxWriter(),
            new FakeBankDetailsRiskEvaluator());

        IFormFile file = CreatePdfFormFile(new byte[] { 1, 2, 3 }, "invoice.pdf");

        var response = await service.UploadAsync(file, cancellationToken: CancellationToken.None);

        var record = await uploadedInvoiceStore.GetByIdAsync(response.InvoiceId, CancellationToken.None);

        Assert.False(record!.IsSupplierMatched);
        Assert.True(record.CanCreateSupplier);
        Assert.Equal(InvoiceStatuses.Parsed, response.Status);
        Assert.Equal(InvoiceMessages.ParsedButRequiresSupplierReview, response.Message);
    }

    [Fact]
    public async Task UploadAsync_ShouldSetCanCreateSupplierFalse_WhenNotMatchedAndMissingSupplierFields()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();

        var service = new InvoiceUploadService(
            new PartialSupplierDataParser(), 
            new NotMatchedSupplierMatcher(),
            new LocalUploadedInvoiceFileStore(),
            uploadedInvoiceStore,
            new FakeExactPostOutboxWriter(),
            new InvoiceParseResultValidator(),
            new SupplierCreateValidator(), 
            new FakeSupplierCreateOutboxWriter(),
            new FakeBankDetailsRiskEvaluator());

        IFormFile file = CreatePdfFormFile(new byte[] { 1, 2, 3 }, "invoice.pdf");

        var response = await service.UploadAsync(file, cancellationToken: CancellationToken.None);

        var record = await uploadedInvoiceStore.GetByIdAsync(response.InvoiceId, CancellationToken.None);

        Assert.False(record!.IsSupplierMatched);
        Assert.False(record.CanCreateSupplier);
        Assert.Equal(InvoiceStatuses.NeedsReview, response.Status);
        Assert.Equal(InvoiceMessages.NeedsReview, response.Message);
    }

    [Fact]
    public async Task UploadAsync_ShouldEnqueueSupplierCreateOutbox_WhenSupplierIsNotMatchedAndCanCreateSupplierIsTrue()
    {
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempRoot);
        Directory.SetCurrentDirectory(tempRoot);

        try
        {
            var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
            var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
            var supplierCreateOutboxWriter = new FakeSupplierCreateOutboxWriter();

            var service = new InvoiceUploadService(
                new FullSupplierDataParser(),
                new NotMatchedSupplierMatcher(),
                new LocalUploadedInvoiceFileStore(),
                uploadedInvoiceStore,
                exactPostOutboxWriter,
                new InvoiceParseResultValidator(),
                new SupplierCreateValidator(), 
                supplierCreateOutboxWriter,
                new FakeBankDetailsRiskEvaluator());

            await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "invoice.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var response = await service.UploadAsync(file, cancellationToken: CancellationToken.None);

            var record = await uploadedInvoiceStore.GetByIdAsync(response.InvoiceId, CancellationToken.None);
            Assert.NotNull(record);
            Assert.False(record!.IsSupplierMatched);
            Assert.True(record.CanCreateSupplier);

            Assert.Single(supplierCreateOutboxWriter.EnqueuedInvoiceIds);
            Assert.Equal(response.InvoiceId, supplierCreateOutboxWriter.EnqueuedInvoiceIds[0]);

            Assert.Equal(0, exactPostOutboxWriter.EnqueueCallsCount);
            Assert.Null(exactPostOutboxWriter.LastEnqueuedInvoiceId);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public async Task UploadAsync_ShouldRequireReview_WhenSupplierIsMatchedButBankDetailsAreNew()
    {
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempRoot);
        Directory.SetCurrentDirectory(tempRoot);

        try
        {
            var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
            var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
            var supplierCreateOutboxWriter = new FakeSupplierCreateOutboxWriter();

            var bankDetailsRiskEvaluator = new FakeBankDetailsRiskEvaluator
            {
                Result = new BankDetailsRiskResult
                {
                    IsSafe = false,
                    IsNewBankDetails = true,
                    HasConflict = false,
                    Reasons = new List<string>
                {
                    "Bank account is new for the matched supplier."
                }
                }
            };

            var service = new InvoiceUploadService(
                new ParserWithBankDetails(),
                new FakeSupplierMatcher(),
                new LocalUploadedInvoiceFileStore(),
                uploadedInvoiceStore,
                exactPostOutboxWriter,
                new InvoiceParseResultValidator(),
                new SupplierCreateValidator(),
                supplierCreateOutboxWriter,
                bankDetailsRiskEvaluator);

            await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "invoice.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var response = await service.UploadAsync(file, cancellationToken: CancellationToken.None);
            var record = await uploadedInvoiceStore.GetByIdAsync(response.InvoiceId, CancellationToken.None);

            Assert.NotNull(record);
            Assert.True(record!.IsSupplierMatched);
            Assert.True(record.RequiresSupplierReview);
            Assert.True(record.HasNewBankDetails);
            Assert.Equal(InvoiceStatuses.NeedsReview, record.Status);
            Assert.Equal("Supplier matched, but bank details are new and require review.", record.SupplierMatchMessage);

            Assert.Equal(InvoiceStatuses.NeedsReview, response.Status);
            Assert.Equal(InvoiceMessages.NeedsReview, response.Message);

            Assert.Equal(0, exactPostOutboxWriter.EnqueueCallsCount);
            Assert.Null(exactPostOutboxWriter.LastEnqueuedInvoiceId);

            Assert.Equal(0, supplierCreateOutboxWriter.EnqueueCallsCount);
            Assert.Null(supplierCreateOutboxWriter.LastEnqueuedInvoiceId);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public async Task UploadAsync_ShouldRequireReview_WhenSupplierIsMatchedButBankDetailsConflict()
    {
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempRoot);
        Directory.SetCurrentDirectory(tempRoot);

        try
        {
            var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
            var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
            var supplierCreateOutboxWriter = new FakeSupplierCreateOutboxWriter();

            var bankDetailsRiskEvaluator = new FakeBankDetailsRiskEvaluator
            {
                Result = new BankDetailsRiskResult
                {
                    IsSafe = false,
                    IsNewBankDetails = false,
                    HasConflict = true,
                    Reasons = new List<string>
                {
                    "Bank account is already linked to another supplier."
                }
                }
            };

            var service = new InvoiceUploadService(
                new ParserWithBankDetails(),
                new FakeSupplierMatcher(),
                new LocalUploadedInvoiceFileStore(),
                uploadedInvoiceStore,
                exactPostOutboxWriter,
                new InvoiceParseResultValidator(),
                new SupplierCreateValidator(),
                supplierCreateOutboxWriter,
                bankDetailsRiskEvaluator);

            await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "invoice.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var response = await service.UploadAsync(file, cancellationToken: CancellationToken.None);
            var record = await uploadedInvoiceStore.GetByIdAsync(response.InvoiceId, CancellationToken.None);

            Assert.NotNull(record);
            Assert.True(record!.IsSupplierMatched);
            Assert.True(record.RequiresSupplierReview);
            Assert.False(record.HasNewBankDetails);
            Assert.Equal(InvoiceStatuses.NeedsReview, record.Status);
            Assert.Equal("Supplier matched, but bank account conflicts with another supplier.", record.SupplierMatchMessage);

            Assert.Equal(InvoiceStatuses.NeedsReview, response.Status);
            Assert.Equal(InvoiceMessages.NeedsReview, response.Message);

            Assert.Equal(0, exactPostOutboxWriter.EnqueueCallsCount);
            Assert.Null(exactPostOutboxWriter.LastEnqueuedInvoiceId);

            Assert.Equal(0, supplierCreateOutboxWriter.EnqueueCallsCount);
            Assert.Null(supplierCreateOutboxWriter.LastEnqueuedInvoiceId);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
            Directory.Delete(tempRoot, true);
        }
    }


    [Fact]
    public async Task UploadAsync_ShouldCreateSupplier_ThenEnqueueExactPost()
    {
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempRoot);
        Directory.SetCurrentDirectory(tempRoot);

        try
        {
            var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
            var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
            var supplierCreateOutboxWriter = new FakeSupplierCreateOutboxWriter();

            var bankDetailsRiskEvaluator = new FakeBankDetailsRiskEvaluator
            {
                Result = new BankDetailsRiskResult
                {
                    IsSafe = true
                }
            };

            var matcher = new FakeSupplierMatcher
            {
                Result = new SupplierMatchResult
                {
                    IsMatched = false,
                    RequiresReview = false,
                    CanCreateSupplier = true
                }
            };

            var service = new InvoiceUploadService(
                new ParserWithBankDetails(),
                matcher,
                new LocalUploadedInvoiceFileStore(),
                uploadedInvoiceStore,
                exactPostOutboxWriter,
                new InvoiceParseResultValidator(),
                new SupplierCreateValidator(),
                supplierCreateOutboxWriter,
                bankDetailsRiskEvaluator);

            await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "invoice.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var response = await service.UploadAsync(file, cancellationToken: CancellationToken.None);

            Assert.Equal(InvoiceStatuses.Parsed, response.Status);

            // supplier creation queued
            Assert.Equal(1, supplierCreateOutboxWriter.EnqueueCallsCount);

            // NOT posted yet
            Assert.Equal(0, exactPostOutboxWriter.EnqueueCallsCount);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
            Directory.Delete(tempRoot, true);
        }
    }
    [Fact]
    public async Task UploadAsync_ShouldReturnExtractionFailed_WhenLlmExtractorReturnsFailed()
    {
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempRoot);
        Directory.SetCurrentDirectory(tempRoot);

        try
        {
            var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
            var service = new InvoiceUploadService(
                new LlmInvoiceParser(new StubFailingLlmExtractor("MalformedJson", "Could not parse LLM response.")),
                new FakeSupplierMatcher(),
                new LocalUploadedInvoiceFileStore(),
                uploadedInvoiceStore,
                new FakeExactPostOutboxWriter(),
                new InvoiceParseResultValidator(),
                new SupplierCreateValidator(),
                new FakeSupplierCreateOutboxWriter(),
                new FakeBankDetailsRiskEvaluator());

            await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "invoice.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var response = await service.UploadAsync(file, cancellationToken: CancellationToken.None);
            var savedRecord = await uploadedInvoiceStore.GetByIdAsync(response.InvoiceId, CancellationToken.None);

            Assert.Equal(InvoiceStatuses.ExtractionFailed, response.Status);
            Assert.NotNull(savedRecord);
            Assert.Equal(InvoiceStatuses.ExtractionFailed, savedRecord!.Status);
            Assert.Equal(InvoiceMessages.ExtractionFailed, savedRecord.Message);
            Assert.NotNull(savedRecord.ExtractionError);
            Assert.Contains("MalformedJson", savedRecord.ExtractionError);
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
    public async Task UploadAsync_ShouldReturnExtractionFailed_WhenLlmExtractorThrows()
    {
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempRoot);
        Directory.SetCurrentDirectory(tempRoot);

        try
        {
            var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
            var service = new InvoiceUploadService(
                new LlmInvoiceParser(new ThrowingLlmExtractor("Unexpected extractor crash")),
                new FakeSupplierMatcher(),
                new LocalUploadedInvoiceFileStore(),
                uploadedInvoiceStore,
                new FakeExactPostOutboxWriter(),
                new InvoiceParseResultValidator(),
                new SupplierCreateValidator(),
                new FakeSupplierCreateOutboxWriter(),
                new FakeBankDetailsRiskEvaluator());

            await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "invoice.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var response = await service.UploadAsync(file, cancellationToken: CancellationToken.None);
            var savedRecord = await uploadedInvoiceStore.GetByIdAsync(response.InvoiceId, CancellationToken.None);

            Assert.Equal(InvoiceStatuses.ExtractionFailed, response.Status);
            Assert.NotNull(savedRecord);
            Assert.Equal(InvoiceStatuses.ExtractionFailed, savedRecord!.Status);
            Assert.Equal(InvoiceMessages.ExtractionFailed, savedRecord.Message);
            Assert.NotNull(savedRecord.ExtractionError);
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
    public async Task UploadAsync_ShouldStoreUploadedBy_WhenCallerIsProvided()
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
                uploadedInvoiceStore,
                new FakeExactPostOutboxWriter(),
                new InvoiceParseResultValidator(),
                new SupplierCreateValidator(),
                new FakeSupplierCreateOutboxWriter(),
                new FakeBankDetailsRiskEvaluator());

            await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            IFormFile file = new FormFile(stream, 0, stream.Length, "file", "invoice.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var response = await service.UploadAsync(file, "uploader@example.com", CancellationToken.None);

            var savedRecord = await uploadedInvoiceStore.GetByIdAsync(response.InvoiceId, CancellationToken.None);
            Assert.NotNull(savedRecord);
            Assert.Equal("uploader@example.com", savedRecord.UploadedBy);
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
    public async Task UploadAsync_ShouldReturnReadyToPost_WhenKvkMatchedAndBankIsSafe()
    {
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);
        Directory.SetCurrentDirectory(tempRoot);
        try
        {
            var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
            var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
            var matcher = new FakeSupplierMatcher
            {
                Result = new SupplierMatchResult
                {
                    IsMatched = true,
                    RequiresReview = true,
                    MatchedBy = SupplierMatchSources.KvK,
                    ExactSupplierId = "exact-kvk-001",
                    Message = "Matched by KvK number."
                }
            };
            var service = new InvoiceUploadService(
                new ParserWithKvkAndBank(),
                matcher,
                new LocalUploadedInvoiceFileStore(),
                uploadedInvoiceStore,
                exactPostOutboxWriter,
                new InvoiceParseResultValidator(),
                new SupplierCreateValidator(),
                new FakeSupplierCreateOutboxWriter(),
                new FakeBankDetailsRiskEvaluator { Result = new BankDetailsRiskResult { IsSafe = true } });

            IFormFile file = CreatePdfFormFile(new byte[] { 1, 2, 3 }, "invoice.pdf");
            var response = await service.UploadAsync(file, cancellationToken: CancellationToken.None);
            var record = await uploadedInvoiceStore.GetByIdAsync(response.InvoiceId, CancellationToken.None);

            Assert.Equal(InvoiceStatuses.ReadyToPost, response.Status);
            Assert.NotNull(record);
            Assert.False(record!.RequiresSupplierReview);
            Assert.Equal(SupplierMatchSources.KvK, record.SupplierMatchedBy);
            Assert.Equal(1, exactPostOutboxWriter.EnqueueCallsCount);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task UploadAsync_ShouldReturnReadyToPost_WhenVatMatchedAndBankIsSafe()
    {
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);
        Directory.SetCurrentDirectory(tempRoot);
        try
        {
            var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
            var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
            var matcher = new FakeSupplierMatcher
            {
                Result = new SupplierMatchResult
                {
                    IsMatched = true,
                    RequiresReview = true,
                    MatchedBy = SupplierMatchSources.Vat,
                    ExactSupplierId = "exact-vat-001",
                    Message = "Matched by VAT number."
                }
            };
            var service = new InvoiceUploadService(
                new ParserWithKvkAndBank(),
                matcher,
                new LocalUploadedInvoiceFileStore(),
                uploadedInvoiceStore,
                exactPostOutboxWriter,
                new InvoiceParseResultValidator(),
                new SupplierCreateValidator(),
                new FakeSupplierCreateOutboxWriter(),
                new FakeBankDetailsRiskEvaluator { Result = new BankDetailsRiskResult { IsSafe = true } });

            IFormFile file = CreatePdfFormFile(new byte[] { 1, 2, 3 }, "invoice.pdf");
            var response = await service.UploadAsync(file, cancellationToken: CancellationToken.None);
            var record = await uploadedInvoiceStore.GetByIdAsync(response.InvoiceId, CancellationToken.None);

            Assert.Equal(InvoiceStatuses.ReadyToPost, response.Status);
            Assert.NotNull(record);
            Assert.False(record!.RequiresSupplierReview);
            Assert.Equal(SupplierMatchSources.Vat, record.SupplierMatchedBy);
            Assert.Equal(1, exactPostOutboxWriter.EnqueueCallsCount);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task UploadAsync_ShouldReturnNeedsReview_WhenKvkMatchedButBankIsNew()
    {
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);
        Directory.SetCurrentDirectory(tempRoot);
        try
        {
            var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
            var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
            var matcher = new FakeSupplierMatcher
            {
                Result = new SupplierMatchResult
                {
                    IsMatched = true,
                    RequiresReview = true,
                    MatchedBy = SupplierMatchSources.KvK,
                    ExactSupplierId = "exact-kvk-001",
                    Message = "Matched by KvK number."
                }
            };
            var service = new InvoiceUploadService(
                new ParserWithKvkAndBank(),
                matcher,
                new LocalUploadedInvoiceFileStore(),
                uploadedInvoiceStore,
                exactPostOutboxWriter,
                new InvoiceParseResultValidator(),
                new SupplierCreateValidator(),
                new FakeSupplierCreateOutboxWriter(),
                new FakeBankDetailsRiskEvaluator
                {
                    Result = new BankDetailsRiskResult
                    {
                        IsSafe = false,
                        IsNewBankDetails = true,
                        Reasons = new List<string> { "Bank account is new for the matched supplier." }
                    }
                });

            IFormFile file = CreatePdfFormFile(new byte[] { 1, 2, 3 }, "invoice.pdf");
            var response = await service.UploadAsync(file, cancellationToken: CancellationToken.None);
            var record = await uploadedInvoiceStore.GetByIdAsync(response.InvoiceId, CancellationToken.None);

            Assert.Equal(InvoiceStatuses.NeedsReview, response.Status);
            Assert.NotNull(record);
            Assert.True(record!.RequiresSupplierReview);
            Assert.Equal(0, exactPostOutboxWriter.EnqueueCallsCount);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task UploadAsync_ShouldReturnNeedsReview_WhenKvkMatchedButBankConflicts()
    {
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);
        Directory.SetCurrentDirectory(tempRoot);
        try
        {
            var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
            var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
            var matcher = new FakeSupplierMatcher
            {
                Result = new SupplierMatchResult
                {
                    IsMatched = true,
                    RequiresReview = true,
                    MatchedBy = SupplierMatchSources.KvK,
                    ExactSupplierId = "exact-kvk-001",
                    Message = "Matched by KvK number."
                }
            };
            var service = new InvoiceUploadService(
                new ParserWithKvkAndBank(),
                matcher,
                new LocalUploadedInvoiceFileStore(),
                uploadedInvoiceStore,
                exactPostOutboxWriter,
                new InvoiceParseResultValidator(),
                new SupplierCreateValidator(),
                new FakeSupplierCreateOutboxWriter(),
                new FakeBankDetailsRiskEvaluator
                {
                    Result = new BankDetailsRiskResult
                    {
                        IsSafe = false,
                        HasConflict = true,
                        Reasons = new List<string> { "Bank account is already linked to another supplier." }
                    }
                });

            IFormFile file = CreatePdfFormFile(new byte[] { 1, 2, 3 }, "invoice.pdf");
            var response = await service.UploadAsync(file, cancellationToken: CancellationToken.None);
            var record = await uploadedInvoiceStore.GetByIdAsync(response.InvoiceId, CancellationToken.None);

            Assert.Equal(InvoiceStatuses.NeedsReview, response.Status);
            Assert.NotNull(record);
            Assert.True(record!.RequiresSupplierReview);
            Assert.Equal(0, exactPostOutboxWriter.EnqueueCallsCount);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task UploadAsync_ShouldReturnNeedsReview_WhenKvkMatchedButNoBankAccount()
    {
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);
        Directory.SetCurrentDirectory(tempRoot);
        try
        {
            var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
            var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
            var matcher = new FakeSupplierMatcher
            {
                Result = new SupplierMatchResult
                {
                    IsMatched = true,
                    RequiresReview = true,
                    MatchedBy = SupplierMatchSources.KvK,
                    ExactSupplierId = "exact-kvk-001",
                    Message = "Matched by KvK number."
                }
            };
            var service = new InvoiceUploadService(
                new ParserWithKvkOnly(),
                matcher,
                new LocalUploadedInvoiceFileStore(),
                uploadedInvoiceStore,
                exactPostOutboxWriter,
                new InvoiceParseResultValidator(),
                new SupplierCreateValidator(),
                new FakeSupplierCreateOutboxWriter(),
                new FakeBankDetailsRiskEvaluator { Result = new BankDetailsRiskResult { IsSafe = true } });

            IFormFile file = CreatePdfFormFile(new byte[] { 1, 2, 3 }, "invoice.pdf");
            var response = await service.UploadAsync(file, cancellationToken: CancellationToken.None);
            var record = await uploadedInvoiceStore.GetByIdAsync(response.InvoiceId, CancellationToken.None);

            Assert.Equal(InvoiceStatuses.NeedsReview, response.Status);
            Assert.NotNull(record);
            Assert.True(record!.RequiresSupplierReview);
            Assert.Equal(0, exactPostOutboxWriter.EnqueueCallsCount);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
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

file sealed class InvalidInvoiceParser : IInvoiceParser
{
    public Task<InvoiceParseResult> ParseAsync(
        FolderInvoiceFile file,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new InvoiceParseResult
        {
            SupplierName = "",
            InvoiceNumber = "",
            InvoiceDate = null,
            TotalAmount = null,
            Currency = ""
        });
    }
}
file sealed class FullSupplierDataParser : IInvoiceParser
{
    public Task<InvoiceParseResult> ParseAsync(
        FolderInvoiceFile file,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new InvoiceParseResult
        {
            SupplierName = "Test Supplier",
            InvoiceNumber = "INV-1",
            InvoiceDate = new DateOnly(2026, 1, 1),
            TotalAmount = 100,
            Currency = "EUR",
            SupplierAddressLine = "Street 1",
            SupplierPostcode = "1234AB",
            SupplierCity = "Amsterdam",
            SupplierCountry = "NL"
        });
    }
}
file sealed class PartialSupplierDataParser : IInvoiceParser
{
    public Task<InvoiceParseResult> ParseAsync(
        FolderInvoiceFile file,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new InvoiceParseResult
        {
            SupplierName = "Test Supplier",
            InvoiceNumber = "INV-1",
            InvoiceDate = new DateOnly(2026, 1, 1),
            TotalAmount = 100,
            Currency = "EUR",
            SupplierAddressLine = null, // missing
            SupplierPostcode = null,
            SupplierCity = "Amsterdam",
            SupplierCountry = null
        });
    }
}
file sealed class NotMatchedSupplierMatcher : ISupplierMatcher
{
    public Task<SupplierMatchResult> MatchAsync(
        InvoiceParseResult parseResult,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SupplierMatchResult
        {
            IsMatched = false,
            RequiresReview = false,
            MatchedBy = null,
            InternalSupplierId = null,
            ExactSupplierId = null,
            Message = "Not matched"
        });
    }
}
file sealed class ParserWithBankDetails : IInvoiceParser
{
    public Task<InvoiceParseResult> ParseAsync(
        FolderInvoiceFile file,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new InvoiceParseResult
        {
            SupplierName = "Demo Supplier",
            InvoiceNumber = "INV-001",
            InvoiceDate = new DateOnly(2026, 4, 1),
            TotalAmount = 123.45m,
            Currency = "EUR",
            SupplierAddressLine = "Main street 1",
            SupplierPostcode = "1234AB",
            SupplierCity = "Amsterdam",
            SupplierCountry = "NL",
            SupplierBankAccount = "NL91ABNA0417164300",
            SupplierBicCode = "ABNANL2A"
        });
    }
}

file sealed class StubFailingLlmExtractor : ILlmInvoiceExtractor
{
    private readonly string _code;
    private readonly string _message;

    public StubFailingLlmExtractor(string code, string message)
    {
        _code = code;
        _message = message;
    }

    public Task<LlmExtractionResult> ExtractAsync(FolderInvoiceFile file, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LlmExtractionResult
        {
            IsSuccessful = false,
            Raw = new LlmRawExtractionResult { RawJson = null },
            Fields = null,
            Metadata = new ExtractionMetadata { Model = "test", ExtractedAtUtc = DateTime.UtcNow, Warnings = [] },
            Error = new ExtractionError { Code = _code, Message = _message }
        });
    }
}

file sealed class ThrowingLlmExtractor : ILlmInvoiceExtractor
{
    private readonly string _message;

    public ThrowingLlmExtractor(string message)
    {
        _message = message;
    }

    public Task<LlmExtractionResult> ExtractAsync(FolderInvoiceFile file, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(_message);
    }
}

file sealed class ParserWithKvkAndBank : IInvoiceParser
{
    public Task<InvoiceParseResult> ParseAsync(
        FolderInvoiceFile file,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new InvoiceParseResult
        {
            SupplierName       = "Acme B.V.",
            InvoiceNumber      = "INV-KVK-001",
            InvoiceDate        = new DateOnly(2026, 5, 1),
            TotalAmount        = 250.00m,
            Currency           = "EUR",
            SupplierKvKNumber  = "12345678",
            SupplierBankAccount = "NL91ABNA0417164300"
        });
    }
}

file sealed class ParserWithKvkOnly : IInvoiceParser
{
    public Task<InvoiceParseResult> ParseAsync(
        FolderInvoiceFile file,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new InvoiceParseResult
        {
            SupplierName      = "Acme B.V.",
            InvoiceNumber     = "INV-KVK-002",
            InvoiceDate       = new DateOnly(2026, 5, 1),
            TotalAmount       = 250.00m,
            Currency          = "EUR",
            SupplierKvKNumber = "12345678",
            SupplierBankAccount = null
        });
    }
}