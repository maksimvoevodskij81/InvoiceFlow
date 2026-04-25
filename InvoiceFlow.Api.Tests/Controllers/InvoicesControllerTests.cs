using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Controllers;
using InvoiceFlow.Api.Features.Invoices;
using InvoiceFlow.Api.Features.Invoices.GetInvoiceDetails;
using InvoiceFlow.Api.Features.Invoices.GetInvoiceStatus;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Features.Invoices.Review;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using InvoiceFlow.Api.Features.Suppliers.Matching;
using InvoiceFlow.Api.Infrastructure;
using InvoiceFlow.Api.Tests.Fakes;
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
            new FakeInvoiceUploadService(),
            new FakeUploadedInvoiceStore(), 
            new InvoiceParseResultValidator(),
            new FakeInvoiceReviewService());

        var request = new ImportInvoicesFromFolderRequest
        {
            FolderPath = string.Empty
        };

        var result = await controller.ImportFromFolder(request, CancellationToken.None);

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
            new FakeInvoiceUploadService(),
            new FakeUploadedInvoiceStore(),
            new InvoiceParseResultValidator(),
            new FakeInvoiceReviewService());

        var folderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(folderPath);
        File.WriteAllText(Path.Combine(folderPath, "notes.txt"), "test");

        try
        {
            var request = new ImportInvoicesFromFolderRequest
            {
                FolderPath = folderPath
            };

            var result = await controller.ImportFromFolder(request, CancellationToken.None);

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
            new FakeInvoiceUploadService(),
            new FakeUploadedInvoiceStore(), 
            new InvoiceParseResultValidator(),
            new FakeInvoiceReviewService());

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
            new FakeInvoiceUploadService(),
            new FakeUploadedInvoiceStore(), 
            new InvoiceParseResultValidator(),
            new FakeInvoiceReviewService());

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
            new FakeInvoiceUploadService(),
            new FakeUploadedInvoiceStore(), 
            new InvoiceParseResultValidator(),
            new FakeInvoiceReviewService());

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
    public async Task Upload_ShouldDelegateToUploadService_AndReturnOkResponse()
    {
        var uploadService = new FakeInvoiceUploadService
        {
            Response = new UploadInvoiceAcceptedResponse
            {
                InvoiceId = "123",
                Status = InvoiceStatuses.Parsed,
                Message = "Invoice parsed successfully."
            }
        };

        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher(),
            uploadService,
            new FakeUploadedInvoiceStore(), 
            new InvoiceParseResultValidator(),
            new FakeInvoiceReviewService());

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

        Assert.Equal("123", response.InvoiceId);
        Assert.Equal(InvoiceStatuses.Parsed, response.Status);
        Assert.Equal("Invoice parsed successfully.", response.Message);
        Assert.Equal(1, uploadService.CallsCount);
        Assert.Same(formFile, uploadService.LastFile);
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
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = "test-hash-123"
        });

        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher(),
            new FakeInvoiceUploadService(),
            uploadedInvoiceStore, 
            new InvoiceParseResultValidator(),
            new FakeInvoiceReviewService());

        var result = await controller.GetStatus("123", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<GetInvoiceStatusResponse>(okResult.Value);

        Assert.Equal("123", response.InvoiceId);
        Assert.Equal(InvoiceStatuses.Processing, response.Status);
        Assert.Equal("Invoice is still being processed.", response.Message);
        Assert.NotNull(response.ReviewSummary);
        Assert.False(response.ReviewSummary.RequiresReview);
        Assert.False(response.ReviewSummary.CanCreateSupplier);
        Assert.False(response.ReviewSummary.HasNewBankDetails);
        Assert.Empty(response.ReviewSummary.Reasons);
        Assert.Equal("Invoice is still being processed.", response.ReviewSummary.CurrentDecisionMessage);
    }

    [Fact]
    public async Task GetStatus_ShouldIncludeReviewSummary_WhenInvoiceHasReviewData()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();

        await uploadedInvoiceStore.SaveAsync(new UploadedInvoiceRecord
        {
            InvoiceId = "review-status-123",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = Path.Combine("temp", "invoice.pdf"),
            Status = InvoiceStatuses.Parsed,
            Message = "Invoice parsed successfully.",
            CreatedAtUtc = DateTime.UtcNow,
            ReviewedAtUtc = DateTime.UtcNow,
            ReviewDecision = ReviewDecisions.Rejected,
            SupplierName = "Demo Supplier",
            InvoiceNumber = "INV-001",
            InvoiceDate = new DateOnly(2026, 4, 1),
            TotalAmount = 123.45m,
            Currency = "EUR",
            IsSupplierMatched = false,
            RequiresSupplierReview = true,
            SupplierMatchedBy = "Name",
            InternalSupplierId = null,
            ExactSupplierId = null,
            SupplierMatchMessage = "Review required.",
            FileHash = "test-hash-123",
            CanCreateSupplier = true,
            HasNewBankDetails = true,
            MatchReasons = new() { "Reason1", "Reason2" }
        });

        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher(),
            new FakeInvoiceUploadService(),
            uploadedInvoiceStore,
            new InvoiceParseResultValidator(),
            new FakeInvoiceReviewService());

        var result = await controller.GetStatus("review-status-123", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<GetInvoiceStatusResponse>(okResult.Value);

        Assert.NotNull(response.ReviewSummary);
        Assert.True(response.ReviewSummary.RequiresReview);
        Assert.True(response.ReviewSummary.CanCreateSupplier);
        Assert.True(response.ReviewSummary.HasNewBankDetails);
        Assert.Equal(new[] { "Reason1", "Reason2" }, response.ReviewSummary.Reasons);
        Assert.Equal(ReviewDecisions.Rejected, response.ReviewSummary.ReviewDecision);
        Assert.NotNull(response.ReviewSummary.ReviewedAtUtc);
        Assert.Equal("Invoice parsed successfully.", response.ReviewSummary.CurrentDecisionMessage);
    }

    [Fact]
    public async Task GetStatus_ShouldReturnNotFound_WhenInvoiceDoesNotExist()
    {
        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher(),
            new FakeInvoiceUploadService(),
            new FakeUploadedInvoiceStore(), 
            new InvoiceParseResultValidator(),
            new FakeInvoiceReviewService());

        var result = await controller.GetStatus("missing-id", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetStatus_ShouldReturnParsedResponse_WithParsedInvoiceSummaryAndSupplierMatch()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();

        await uploadedInvoiceStore.SaveAsync(new UploadedInvoiceRecord
        {
            InvoiceId = "parsed-123",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = Path.Combine("temp", "invoice.pdf"),
            Status = InvoiceStatuses.Parsed,
            Message = "Invoice parsed successfully.",
            CreatedAtUtc = DateTime.UtcNow,
            SupplierName = "Demo Supplier",
            InvoiceNumber = "INV-001",
            InvoiceDate = new DateOnly(2026, 4, 1),
            TotalAmount = 123.45m,
            Currency = "EUR",
            IsSupplierMatched = true,
            RequiresSupplierReview = false,
            SupplierMatchedBy = "BankAccount",
            InternalSupplierId = "internal-supplier-001",
            ExactSupplierId = "exact-supplier-001",
            SupplierMatchMessage = "Supplier matched successfully.",
            FileHash = "test-hash-123"
        });

        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher(),
            new FakeInvoiceUploadService(),
            uploadedInvoiceStore,
            new InvoiceParseResultValidator(),
            new FakeInvoiceReviewService());

        var result = await controller.GetStatus("parsed-123", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<GetInvoiceStatusResponse>(okResult.Value);

        Assert.Equal("parsed-123", response.InvoiceId);
        Assert.Equal(InvoiceStatuses.Parsed, response.Status);
        Assert.Equal("Invoice parsed successfully.", response.Message);
        Assert.Equal("Demo Supplier", response.SupplierName);
        Assert.Equal("INV-001", response.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 4, 1), response.InvoiceDate);
        Assert.Equal(123.45m, response.TotalAmount);
        Assert.Equal("EUR", response.Currency);
        Assert.True(response.IsSupplierMatched);
        Assert.False(response.RequiresSupplierReview);
        Assert.Equal("BankAccount", response.SupplierMatchedBy);
        Assert.Equal("internal-supplier-001", response.InternalSupplierId);
        Assert.Equal("exact-supplier-001", response.ExactSupplierId);
        Assert.Equal("Supplier matched successfully.", response.SupplierMatchMessage);
    }

    [Fact]
    public async Task GetStatus_ShouldReturnReviewFlags_WhenSupplierReviewIsRequired()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();

        await uploadedInvoiceStore.SaveAsync(new UploadedInvoiceRecord
        {
            InvoiceId = "review-123",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = Path.Combine("temp", "invoice.pdf"),
            Status = InvoiceStatuses.Parsed,
            Message = "Invoice parsed successfully.",
            CreatedAtUtc = DateTime.UtcNow,
            SupplierName = "Demo Supplier",
            InvoiceNumber = "INV-001",
            InvoiceDate = new DateOnly(2026, 4, 1),
            TotalAmount = 123.45m,
            Currency = "EUR",
            IsSupplierMatched = false,
            RequiresSupplierReview = true,
            SupplierMatchedBy = "Name",
            InternalSupplierId = null,
            ExactSupplierId = null,
            SupplierMatchMessage = "Multiple supplier candidates found. Review required.",
            FileHash = "test-hash-123"
        });

        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher(),
            new FakeInvoiceUploadService(),
            uploadedInvoiceStore,
            new InvoiceParseResultValidator(),
            new FakeInvoiceReviewService());

        var result = await controller.GetStatus("review-123", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<GetInvoiceStatusResponse>(okResult.Value);

        Assert.Equal("review-123", response.InvoiceId);
        Assert.Equal(InvoiceStatuses.Parsed, response.Status);
        Assert.False(response.IsSupplierMatched);
        Assert.True(response.RequiresSupplierReview);
        Assert.Equal("Name", response.SupplierMatchedBy);
        Assert.Null(response.InternalSupplierId);
        Assert.Null(response.ExactSupplierId);
        Assert.Equal("Multiple supplier candidates found. Review required.", response.SupplierMatchMessage);
    }

    [Fact]
    public async Task GetById_ShouldReturnInvoiceDetails_WhenInvoiceExists()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();

        await uploadedInvoiceStore.SaveAsync(new UploadedInvoiceRecord
        {
            InvoiceId = "details-123",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = Path.Combine("temp", "invoice.pdf"),
            Status = InvoiceStatuses.ReadyToPost,
            Message = InvoiceMessages.ReadyToPost,
            CreatedAtUtc = new DateTime(2026, 4, 8, 10, 30, 0, DateTimeKind.Utc),
            FileHash = "test-hash-details-123",
            SupplierName = "Demo Supplier",
            InvoiceNumber = "INV-001",
            InvoiceDate = new DateOnly(2026, 4, 1),
            TotalAmount = 123.45m,
            Currency = "EUR",
            IsSupplierMatched = true,
            RequiresSupplierReview = false,
            SupplierMatchedBy = SupplierMatchSources.BankAccount,
            InternalSupplierId = "internal-supplier-001",
            ExactSupplierId = "exact-supplier-001",
            SupplierMatchMessage = "Supplier matched successfully."
        });

        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher(),
            new FakeInvoiceUploadService(),
            uploadedInvoiceStore, 
            new InvoiceParseResultValidator(),
            new FakeInvoiceReviewService());

        var result = await controller.GetById("details-123", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<GetInvoiceDetailsResponse>(okResult.Value);

        Assert.Equal("details-123", response.InvoiceId);
        Assert.Equal("invoice.pdf", response.OriginalFileName);
        Assert.Equal(new DateTime(2026, 4, 8, 10, 30, 0, DateTimeKind.Utc), response.CreatedAtUtc);
        Assert.Equal(InvoiceStatuses.ReadyToPost, response.Status);
        Assert.Equal(InvoiceMessages.ReadyToPost, response.Message);
        Assert.Equal("Demo Supplier", response.SupplierName);
        Assert.Equal("INV-001", response.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 4, 1), response.InvoiceDate);
        Assert.Equal(123.45m, response.TotalAmount);
        Assert.Equal("EUR", response.Currency);
        Assert.True(response.IsSupplierMatched);
        Assert.False(response.RequiresSupplierReview);
        Assert.Equal("BankAccount", response.SupplierMatchedBy);
        Assert.Equal("internal-supplier-001", response.InternalSupplierId);
        Assert.Equal("exact-supplier-001", response.ExactSupplierId);
        Assert.Equal("Supplier matched successfully.", response.SupplierMatchMessage);
        Assert.NotNull(response.ReviewSummary);
        Assert.False(response.ReviewSummary.RequiresReview);
        Assert.False(response.ReviewSummary.CanCreateSupplier);
        Assert.False(response.ReviewSummary.HasNewBankDetails);
        Assert.Empty(response.ReviewSummary.Reasons);
        Assert.Equal(InvoiceMessages.ReadyToPost, response.ReviewSummary.CurrentDecisionMessage);
    }

    [Fact]
    public async Task GetById_ShouldIncludeReviewSummary_WhenInvoiceHasReviewData()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();

        await uploadedInvoiceStore.SaveAsync(new UploadedInvoiceRecord
        {
            InvoiceId = "review-details-123",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = Path.Combine("temp", "invoice.pdf"),
            Status = InvoiceStatuses.Parsed,
            Message = "Invoice parsed successfully.",
            CreatedAtUtc = DateTime.UtcNow,
            ReviewedAtUtc = DateTime.UtcNow,
            ReviewDecision = ReviewDecisions.Approved,
            FileHash = "test-hash-details-123",
            SupplierName = "Demo Supplier",
            InvoiceNumber = "INV-001",
            InvoiceDate = new DateOnly(2026, 4, 1),
            TotalAmount = 123.45m,
            Currency = "EUR",
            IsSupplierMatched = false,
            RequiresSupplierReview = true,
            SupplierMatchedBy = "Name",
            InternalSupplierId = null,
            ExactSupplierId = null,
            SupplierMatchMessage = "Review required.",
            CanCreateSupplier = true,
            HasNewBankDetails = true,
            MatchReasons = new() { "Reason1", "Reason2" }
        });

        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher(),
            new FakeInvoiceUploadService(),
            uploadedInvoiceStore,
            new InvoiceParseResultValidator(),
            new FakeInvoiceReviewService());

        var result = await controller.GetById("review-details-123", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<GetInvoiceDetailsResponse>(okResult.Value);

        Assert.NotNull(response.ReviewSummary);
        Assert.True(response.ReviewSummary.RequiresReview);
        Assert.True(response.ReviewSummary.CanCreateSupplier);
        Assert.True(response.ReviewSummary.HasNewBankDetails);
        Assert.Equal(new[] { "Reason1", "Reason2" }, response.ReviewSummary.Reasons);
        Assert.Equal(ReviewDecisions.Approved, response.ReviewSummary.ReviewDecision);
        Assert.NotNull(response.ReviewSummary.ReviewedAtUtc);
        Assert.Equal("Invoice parsed successfully.", response.ReviewSummary.CurrentDecisionMessage);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenInvoiceDoesNotExist()
    {
        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher(),
            new FakeInvoiceUploadService(),
            new FakeUploadedInvoiceStore(), 
            new InvoiceParseResultValidator(),
            new FakeInvoiceReviewService());

        var result = await controller.GetById("missing-details-id", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task ImportFromFolder_ShouldReturnInvalid_WhenParsedInvoiceIsMissingRequiredFields()
    {
        var folderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(folderPath);
        var filePath = Path.Combine(folderPath, "invoice.pdf");
        await File.WriteAllBytesAsync(filePath, new byte[] { 1, 2, 3 });

        try
        {
            var controller = new InvoicesController(
                new LocalInvoiceFolderReader(),
                new InvalidInvoiceParser(),
                new FakeSupplierMatcher(),
                new FakeInvoiceUploadService(),
                new FakeUploadedInvoiceStore(),
                new InvoiceParseResultValidator(),
            new FakeInvoiceReviewService());

            var request = new ImportInvoicesFromFolderRequest
            {
                FolderPath = folderPath
            };

            var result = await controller.ImportFromFolder(request, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ImportInvoicesFromFolderResponse>(okResult.Value);

            Assert.Equal(InvoiceStatuses.Invalid, response.Status);
            Assert.False(response.IsSupplierMatched);
            Assert.False(response.RequiresSupplierReview);
            Assert.Contains("Missing required fields:", response.SupplierMatchMessage);
        }
        finally
        {
            Directory.Delete(folderPath, true);
        }
    }


    [Fact]
    public async Task ImportFromFolder_ShouldReturnReadyToPost_WhenInvoiceIsValidAndSupplierIsMatchedWithoutReview()
    {
        var folderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(folderPath);
        var filePath = Path.Combine(folderPath, "invoice.pdf");
        await File.WriteAllBytesAsync(filePath, new byte[] { 1, 2, 3 });

        try
        {
            var controller = new InvoicesController(
                new LocalInvoiceFolderReader(),
                new FakeInvoiceParser(),
                new FakeSupplierMatcher(),
                new FakeInvoiceUploadService(),
                new FakeUploadedInvoiceStore(),
                new InvoiceParseResultValidator(),
            new FakeInvoiceReviewService());

            var request = new ImportInvoicesFromFolderRequest
            {
                FolderPath = folderPath
            };

            var result = await controller.ImportFromFolder(request, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ImportInvoicesFromFolderResponse>(okResult.Value);

            Assert.Equal(InvoiceStatuses.ReadyToPost, response.Status);
            Assert.True(response.IsSupplierMatched);
            Assert.False(response.RequiresSupplierReview);
            Assert.Equal("exact-supplier-001", response.ExactSupplierId);
        }
        finally
        {
            Directory.Delete(folderPath, true);
        }
    }


    [Fact]
    public async Task Upload_ShouldReturnInvalidResponse_WithMissingFields_WhenUploadServiceReturnsInvalid()
    {
        var uploadService = new FakeInvoiceUploadService
        {
            Response = new UploadInvoiceAcceptedResponse
            {
                InvoiceId = "invalid-123",
                Status = InvoiceStatuses.Invalid,
                Message = "Missing required fields: SupplierName, InvoiceNumber",
                MissingFields = new List<string>
            {
                "SupplierName",
                "InvoiceNumber"
            }
            }
        };

        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher(),
            uploadService,
            new FakeUploadedInvoiceStore(),
            new InvoiceParseResultValidator(),
            new FakeInvoiceReviewService());

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

        Assert.Equal("invalid-123", response.InvoiceId);
        Assert.Equal(InvoiceStatuses.Invalid, response.Status);
        Assert.Equal("Missing required fields: SupplierName, InvoiceNumber", response.Message);
        Assert.Equal(2, response.MissingFields.Count);
        Assert.Contains("SupplierName", response.MissingFields);
        Assert.Contains("InvoiceNumber", response.MissingFields);
    }

    [Fact]
    public async Task ApproveReview_ShouldReturnOk_WhenApprovalSucceeds()
    {
        var reviewService = new FakeInvoiceReviewService();

        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher(),
            new FakeInvoiceUploadService(),
            new FakeUploadedInvoiceStore(),
            new InvoiceParseResultValidator(),
            reviewService);

        var result = await controller.ApproveReview("invoice-123", CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.Equal(1, reviewService.ApproveCallsCount);
        Assert.Equal("invoice-123", reviewService.LastApprovedInvoiceId);
    }

    [Fact]
    public async Task ApproveReview_ShouldReturnNotFound_WhenInvoiceDoesNotExist()
    {
        var reviewService = new FakeInvoiceReviewService
        {
            ApproveException = new KeyNotFoundException("Not found")
        };

        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher(),
            new FakeInvoiceUploadService(),
            new FakeUploadedInvoiceStore(),
            new InvoiceParseResultValidator(),
            reviewService);

        var result = await controller.ApproveReview("missing-invoice", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ApproveReview_ShouldReturnBadRequest_WhenInvoiceIsNotInNeedsReviewOrHasNoSafeNextStep()
    {
        var reviewService = new FakeInvoiceReviewService
        {
            ApproveException = new InvalidOperationException("Invalid state")
        };

        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher(),
            new FakeInvoiceUploadService(),
            new FakeUploadedInvoiceStore(),
            new InvoiceParseResultValidator(),
            reviewService);

        var result = await controller.ApproveReview("invalid-invoice", CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task RejectReview_ShouldReturnOk_WhenRejectionSucceeds()
    {
        var reviewService = new FakeInvoiceReviewService();

        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher(),
            new FakeInvoiceUploadService(),
            new FakeUploadedInvoiceStore(),
            new InvoiceParseResultValidator(),
            reviewService);

        var result = await controller.RejectReview("invoice-123", CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.Equal(1, reviewService.RejectCallsCount);
        Assert.Equal("invoice-123", reviewService.LastRejectedInvoiceId);
    }

    [Fact]
    public async Task RejectReview_ShouldReturnNotFound_WhenInvoiceDoesNotExist()
    {
        var reviewService = new FakeInvoiceReviewService
        {
            RejectException = new KeyNotFoundException("Not found")
        };

        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher(),
            new FakeInvoiceUploadService(),
            new FakeUploadedInvoiceStore(),
            new InvoiceParseResultValidator(),
            reviewService);

        var result = await controller.RejectReview("missing-invoice", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RejectReview_ShouldReturnBadRequest_WhenInvoiceIsNotInNeedsReview()
    {
        var reviewService = new FakeInvoiceReviewService
        {
            RejectException = new InvalidOperationException("Invalid state")
        };

        var controller = new InvoicesController(
            new LocalInvoiceFolderReader(),
            new FakeInvoiceParser(),
            new FakeSupplierMatcher(),
            new FakeInvoiceUploadService(),
            new FakeUploadedInvoiceStore(),
            new InvoiceParseResultValidator(),
            reviewService);

        var result = await controller.RejectReview("invalid-invoice", CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
    }
}

file sealed class InvalidInvoiceParser : IInvoiceParser
{
    public Task<InvoiceParseResult> ParseAsync(FolderInvoiceFile file, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new InvoiceParseResult
        {
            SupplierName = string.Empty,
            InvoiceNumber = string.Empty,
            InvoiceDate = null,
            TotalAmount = null,
            Currency = string.Empty
        });
    }
}
