using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Invoices;
using InvoiceFlow.Api.Features.Invoices.Extraction;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Features.Invoices.RetryExtraction;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using InvoiceFlow.Api.Features.Suppliers.Matching;
using InvoiceFlow.Api.Infrastructure;
using InvoiceFlow.Api.Tests.Fakes;

namespace InvoiceFlow.Api.Tests.Features.Invoices.RetryExtraction;

public sealed class InvoiceRetryServiceTests
{
    [Fact]
    public async Task RetryExtractionAsync_ShouldTransitionToReadyToPost_WhenExtractionSucceedsAndSupplierMatched()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var invoice = BuildExtractionFailedRecord("retry-1");
        await uploadedInvoiceStore.SaveAsync(invoice, CancellationToken.None);

        var service = BuildService(uploadedInvoiceStore, new FakeInvoiceParser(), new FakeSupplierMatcher());

        var response = await service.RetryExtractionAsync("retry-1", CancellationToken.None);

        Assert.Equal("retry-1", response.InvoiceId);
        Assert.Equal(InvoiceStatuses.ReadyToPost, response.Status);

        var updated = await uploadedInvoiceStore.GetByIdAsync("retry-1", CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(InvoiceStatuses.ReadyToPost, updated.Status);
    }

    [Fact]
    public async Task RetryExtractionAsync_ShouldTransitionToNeedsReview_WhenExtractionSucceedsButSupplierRequiresReview()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var invoice = BuildExtractionFailedRecord("retry-2");
        await uploadedInvoiceStore.SaveAsync(invoice, CancellationToken.None);

        var matcher = new FakeSupplierMatcher
        {
            Result = new SupplierMatchResult
            {
                IsMatched = true,
                RequiresReview = true,
                ExactSupplierId = "exact-1",
                Message = "Requires review."
            }
        };

        var service = BuildService(uploadedInvoiceStore, new FakeInvoiceParser(), matcher);

        var response = await service.RetryExtractionAsync("retry-2", CancellationToken.None);

        Assert.Equal("retry-2", response.InvoiceId);
        Assert.Equal(InvoiceStatuses.NeedsReview, response.Status);

        var updated = await uploadedInvoiceStore.GetByIdAsync("retry-2", CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(InvoiceStatuses.NeedsReview, updated.Status);
    }

    [Fact]
    public async Task RetryExtractionAsync_ShouldRemainExtractionFailed_WhenLlmExtractorFailsAgain()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var invoice = BuildExtractionFailedRecord("retry-3");
        await uploadedInvoiceStore.SaveAsync(invoice, CancellationToken.None);

        var service = BuildService(
            uploadedInvoiceStore,
            new LlmInvoiceParser(new StubFailingLlmExtractor("Timeout", "LLM timed out.")),
            new FakeSupplierMatcher());

        var response = await service.RetryExtractionAsync("retry-3", CancellationToken.None);

        Assert.Equal("retry-3", response.InvoiceId);
        Assert.Equal(InvoiceStatuses.ExtractionFailed, response.Status);

        var updated = await uploadedInvoiceStore.GetByIdAsync("retry-3", CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(InvoiceStatuses.ExtractionFailed, updated.Status);
        Assert.NotNull(updated.ExtractionError);
        Assert.Contains("Timeout", updated.ExtractionError);
    }

    [Fact]
    public async Task RetryExtractionAsync_ShouldThrow_WhenInvoiceNotFound()
    {
        var store = new FakeUploadedInvoiceStore();
        var service = BuildService(store, new FakeInvoiceParser(), new FakeSupplierMatcher());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.RetryExtractionAsync("not-found", CancellationToken.None));
    }

    [Fact]
    public async Task RetryExtractionAsync_ShouldThrow_WhenInvoiceIsNotExtractionFailed()
    {
        var store = new FakeUploadedInvoiceStore();
        await store.SaveAsync(new UploadedInvoiceRecord
        {
            InvoiceId        = "wrong-status",
            OriginalFileName = "invoice.pdf",
            StoredFilePath   = "path",
            Status           = InvoiceStatuses.NeedsReview,
            Message          = InvoiceMessages.NeedsReview,
            CreatedAtUtc     = DateTime.UtcNow,
            FileHash         = "hash"
        }, CancellationToken.None);

        var service = BuildService(store, new FakeInvoiceParser(), new FakeSupplierMatcher());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RetryExtractionAsync("wrong-status", CancellationToken.None));

        Assert.Contains(InvoiceStatuses.ExtractionFailed, ex.Message);
    }

    private static InvoiceRetryService BuildService(
        FakeUploadedInvoiceStore store,
        IInvoiceParser parser,
        ISupplierMatcher matcher)
    {
        return new InvoiceRetryService(
            store,
            parser,
            matcher,
            new FakeBankDetailsRiskEvaluator(),
            new InvoiceParseResultValidator(),
            new SupplierCreateValidator(),
            new FakeExactPostOutboxWriter(),
            new FakeSupplierCreateOutboxWriter());
    }

    private static UploadedInvoiceRecord BuildExtractionFailedRecord(string invoiceId) => new()
    {
        InvoiceId        = invoiceId,
        OriginalFileName = "invoice.pdf",
        StoredFilePath   = "path/to/invoice.pdf",
        Status           = InvoiceStatuses.ExtractionFailed,
        Message          = InvoiceMessages.ExtractionFailed,
        CreatedAtUtc     = DateTime.UtcNow,
        FileHash         = "hash"
    };
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
            Raw      = new LlmRawExtractionResult { RawJson = null },
            Fields   = null,
            Metadata = new ExtractionMetadata { Model = "test", ExtractedAtUtc = DateTime.UtcNow, Warnings = [] },
            Error    = new ExtractionError { Code = _code, Message = _message }
        });
    }
}
