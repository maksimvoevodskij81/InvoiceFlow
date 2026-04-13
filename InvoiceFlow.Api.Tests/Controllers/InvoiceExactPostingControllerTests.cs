using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Controllers;
using InvoiceFlow.Api.Features.Exact;
using InvoiceFlow.Api.Features.Invoices;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using InvoiceFlow.Api.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Api.Tests.Controllers;

public sealed class InvoiceExactPostingControllerTests
{
    [Fact]
    public async Task RetryExactPost_ShouldQueueRetry_WhenInvoiceHasFailedExactPosting()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var outboxWriter = new FakeExactPostOutboxWriter();

        await uploadedInvoiceStore.SaveAsync(new UploadedInvoiceRecord
        {
            InvoiceId = "invoice-123",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = Path.Combine("temp", "invoice.pdf"),
            Status = InvoiceStatuses.Parsed,
            Message = InvoiceMessages.ParsedSuccessfully,
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = "hash-123",
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
            SupplierMatchMessage = "Supplier matched successfully.",
            ExactPostingStatus = ExactPostingStatuses.Failed,
            ExactDocumentId = null,
            PostedToExactAtUtc = null,
            ExactPostingError = "Timeout"
        });

        var controller = new InvoiceExactPostingController(uploadedInvoiceStore, outboxWriter);

        var result = await controller.RetryExactPost("invoice-123", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<RetryExactPostResponse>(okResult.Value);

        Assert.Equal("invoice-123", response.InvoiceId);
        Assert.Equal(ExactPostingStatuses.Queued, response.ExactPostingStatus);
        Assert.Equal("Exact posting retry queued.", response.Message);
        Assert.Equal(1, outboxWriter.RequeueCallsCount);
        Assert.Equal("invoice-123", outboxWriter.LastRequeuedInvoiceId);

        var savedInvoice = await uploadedInvoiceStore.GetByIdAsync("invoice-123", CancellationToken.None);

        Assert.NotNull(savedInvoice);
        Assert.Equal(ExactPostingStatuses.Queued, savedInvoice.ExactPostingStatus);
        Assert.Null(savedInvoice.ExactPostingError);
        Assert.Null(savedInvoice.ExactDocumentId);
        Assert.Null(savedInvoice.PostedToExactAtUtc);
    }

    [Fact]
    public async Task RetryExactPost_ShouldReturnBadRequest_WhenInvoiceIsNotReadyForExactPosting()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var outboxWriter = new FakeExactPostOutboxWriter();

        await uploadedInvoiceStore.SaveAsync(new UploadedInvoiceRecord
        {
            InvoiceId = "invoice-124",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = Path.Combine("temp", "invoice.pdf"),
            Status = InvoiceStatuses.Parsed,
            Message = "Invoice parsed successfully.",
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = "hash-124",
            IsSupplierMatched = false,
            RequiresSupplierReview = true,
            ExactSupplierId = null,
            ExactPostingStatus = ExactPostingStatuses.Failed
        });

        var controller = new InvoiceExactPostingController(uploadedInvoiceStore, outboxWriter);

        var result = await controller.RetryExactPost("invoice-124", CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal("Invoice is not ready for Exact posting.", badRequestResult.Value);
        Assert.Equal(0, outboxWriter.RequeueCallsCount);
    }

    [Fact]
    public async Task RetryExactPost_ShouldReturnNotFound_WhenInvoiceDoesNotExist()
    {
        var controller = new InvoiceExactPostingController(
            new FakeUploadedInvoiceStore(),
            new FakeExactPostOutboxWriter());

        var result = await controller.RetryExactPost("missing-id", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task RetryExactPost_ShouldReturnBadRequest_WhenExactPostingIsNotFailed()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var outboxWriter = new FakeExactPostOutboxWriter();

        await uploadedInvoiceStore.SaveAsync(new UploadedInvoiceRecord
        {
            InvoiceId = "invoice-125",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = Path.Combine("temp", "invoice.pdf"),
            Status = InvoiceStatuses.Parsed,
            Message = "Invoice parsed successfully.",
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = "hash-125",
            IsSupplierMatched = true,
            RequiresSupplierReview = false,
            ExactSupplierId = "exact-supplier-001",
            ExactPostingStatus = ExactPostingStatuses.Posted
        });

        var controller = new InvoiceExactPostingController(uploadedInvoiceStore, outboxWriter);

        var result = await controller.RetryExactPost("invoice-125", CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal("Only failed Exact postings can be retried.", badRequestResult.Value);
        Assert.Equal(0, outboxWriter.RequeueCallsCount);
    }
}