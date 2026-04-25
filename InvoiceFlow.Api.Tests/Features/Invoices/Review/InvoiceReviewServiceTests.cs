using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Invoices;
using InvoiceFlow.Api.Features.Invoices.Review;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using InvoiceFlow.Api.Tests.Fakes;

namespace InvoiceFlow.Api.Tests.Features.Invoices.Review;

public sealed class InvoiceReviewServiceTests
{
    [Fact]
    public async Task ApproveAsync_ShouldQueueExactPost_WhenInvoiceHasExactSupplierId()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
        var supplierCreateOutboxWriter = new FakeSupplierCreateOutboxWriter();

        var service = new InvoiceReviewService(
            uploadedInvoiceStore,
            exactPostOutboxWriter,
            supplierCreateOutboxWriter);

        var invoice = new UploadedInvoiceRecord
        {
            InvoiceId = "invoice-1",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = "path",
            Status = InvoiceStatuses.NeedsReview,
            Message = "Needs review",
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = "hash",
            SupplierName = "Supplier",
            InvoiceNumber = "INV-001",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalAmount = 100.00m,
            Currency = "EUR",
            IsSupplierMatched = true,
            RequiresSupplierReview = true,
            SupplierMatchedBy = "Manual",
            InternalSupplierId = "internal-1",
            ExactSupplierId = "exact-1",
            SupplierMatchMessage = "Matched",
            CanCreateSupplier = false,
            SupplierAddressLine = "Address",
            SupplierPostcode = "12345",
            SupplierCity = "City",
            SupplierCountry = "NL",
            SupplierBankAccount = "IBAN",
            SupplierBicCode = "BIC",
            HasNewBankDetails = true,
            MatchReasons = new() { "Reason1" }
        };

        await uploadedInvoiceStore.SaveAsync(invoice, CancellationToken.None);

        await service.ApproveAsync("invoice-1", CancellationToken.None);

        var updatedInvoice = await uploadedInvoiceStore.GetByIdAsync("invoice-1", CancellationToken.None);

        Assert.NotNull(updatedInvoice);
        Assert.Equal(InvoiceStatuses.ReadyToPost, updatedInvoice.Status);
        Assert.Equal(InvoiceMessages.ReadyToPost, updatedInvoice.Message);
        Assert.False(updatedInvoice.RequiresSupplierReview);
        Assert.False(updatedInvoice.HasNewBankDetails);
        Assert.Empty(updatedInvoice.MatchReasons);
        Assert.Equal(1, exactPostOutboxWriter.EnqueueCallsCount);
        Assert.Equal("invoice-1", exactPostOutboxWriter.LastEnqueuedInvoiceId);
        Assert.Equal(0, supplierCreateOutboxWriter.EnqueueCallsCount);
    }

    [Fact]
    public async Task ApproveAsync_ShouldQueueSupplierCreate_WhenInvoiceCanCreateSupplier()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
        var supplierCreateOutboxWriter = new FakeSupplierCreateOutboxWriter();

        var service = new InvoiceReviewService(
            uploadedInvoiceStore,
            exactPostOutboxWriter,
            supplierCreateOutboxWriter);

        var invoice = new UploadedInvoiceRecord
        {
            InvoiceId = "invoice-2",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = "path",
            Status = InvoiceStatuses.NeedsReview,
            Message = "Needs review",
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = "hash",
            SupplierName = "Supplier",
            InvoiceNumber = "INV-002",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalAmount = 200.00m,
            Currency = "EUR",
            IsSupplierMatched = false,
            RequiresSupplierReview = true,
            SupplierMatchedBy = null,
            InternalSupplierId = null,
            ExactSupplierId = null,
            SupplierMatchMessage = null,
            CanCreateSupplier = true,
            SupplierAddressLine = "Address",
            SupplierPostcode = "12345",
            SupplierCity = "City",
            SupplierCountry = "NL",
            SupplierBankAccount = "IBAN",
            SupplierBicCode = "BIC",
            HasNewBankDetails = true,
            MatchReasons = new() { "Reason1" }
        };

        await uploadedInvoiceStore.SaveAsync(invoice, CancellationToken.None);

        await service.ApproveAsync("invoice-2", CancellationToken.None);

        var updatedInvoice = await uploadedInvoiceStore.GetByIdAsync("invoice-2", CancellationToken.None);

        Assert.NotNull(updatedInvoice);
        Assert.Equal(InvoiceStatuses.Parsed, updatedInvoice.Status);
        Assert.Equal(InvoiceMessages.ParsedSuccessfully, updatedInvoice.Message);
        Assert.False(updatedInvoice.RequiresSupplierReview);
        Assert.False(updatedInvoice.HasNewBankDetails);
        Assert.Empty(updatedInvoice.MatchReasons);
        Assert.Equal(0, exactPostOutboxWriter.EnqueueCallsCount);
        Assert.Equal(1, supplierCreateOutboxWriter.EnqueueCallsCount);
        Assert.Equal("invoice-2", supplierCreateOutboxWriter.LastEnqueuedInvoiceId);
    }

    [Fact]
    public async Task ApproveAsync_ShouldThrow_WhenInvoiceIsNotInNeedsReview()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
        var supplierCreateOutboxWriter = new FakeSupplierCreateOutboxWriter();

        var service = new InvoiceReviewService(
            uploadedInvoiceStore,
            exactPostOutboxWriter,
            supplierCreateOutboxWriter);

        var invoice = new UploadedInvoiceRecord
        {
            InvoiceId = "invoice-3",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = "path",
            Status = InvoiceStatuses.Parsed,
            Message = "Parsed",
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = "hash",
            SupplierName = "Supplier",
            InvoiceNumber = "INV-003",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalAmount = 300.00m,
            Currency = "EUR",
            IsSupplierMatched = false,
            RequiresSupplierReview = false,
            SupplierMatchedBy = null,
            InternalSupplierId = null,
            ExactSupplierId = null,
            SupplierMatchMessage = null,
            CanCreateSupplier = false,
            SupplierAddressLine = null,
            SupplierPostcode = null,
            SupplierCity = null,
            SupplierCountry = null,
            SupplierBankAccount = null,
            SupplierBicCode = null,
            HasNewBankDetails = false,
            MatchReasons = new()
        };

        await uploadedInvoiceStore.SaveAsync(invoice, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ApproveAsync("invoice-3", CancellationToken.None));

        Assert.Contains("not in 'NeedsReview' status", exception.Message);
    }

    [Fact]
    public async Task ApproveAsync_ShouldSetReviewAuditFields()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
        var supplierCreateOutboxWriter = new FakeSupplierCreateOutboxWriter();

        var service = new InvoiceReviewService(
            uploadedInvoiceStore,
            exactPostOutboxWriter,
            supplierCreateOutboxWriter);

        var invoice = new UploadedInvoiceRecord
        {
            InvoiceId = "invoice-8",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = "path",
            Status = InvoiceStatuses.NeedsReview,
            Message = InvoiceMessages.NeedsReview,
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = "hash",
            SupplierName = "Supplier",
            InvoiceNumber = "INV-008",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalAmount = 800.00m,
            Currency = "EUR",
            IsSupplierMatched = true,
            RequiresSupplierReview = true,
            SupplierMatchedBy = "Manual",
            InternalSupplierId = "internal-8",
            ExactSupplierId = "exact-8",
            SupplierMatchMessage = "Matched",
            CanCreateSupplier = false,
            SupplierAddressLine = "Address",
            SupplierPostcode = "12345",
            SupplierCity = "City",
            SupplierCountry = "NL",
            SupplierBankAccount = "IBAN",
            SupplierBicCode = "BIC",
            HasNewBankDetails = true,
            MatchReasons = new() { "Reason1" }
        };

        await uploadedInvoiceStore.SaveAsync(invoice, CancellationToken.None);

        var beforeReview = DateTime.UtcNow;
        await service.ApproveAsync("invoice-8", CancellationToken.None);
        var afterReview = DateTime.UtcNow;

        var updatedInvoice = await uploadedInvoiceStore.GetByIdAsync("invoice-8", CancellationToken.None);

        Assert.NotNull(updatedInvoice);
        Assert.Equal(InvoiceStatuses.ReadyToPost, updatedInvoice.Status);
        Assert.Equal(InvoiceMessages.ReadyToPost, updatedInvoice.Message);
        Assert.Equal(ReviewDecisions.Approved, updatedInvoice.ReviewDecision);
        Assert.NotNull(updatedInvoice.ReviewedAtUtc);
        Assert.True(updatedInvoice.ReviewedAtUtc.Value >= beforeReview);
        Assert.True(updatedInvoice.ReviewedAtUtc.Value <= afterReview);
        Assert.Equal(1, exactPostOutboxWriter.EnqueueCallsCount);
        Assert.Equal("invoice-8", exactPostOutboxWriter.LastEnqueuedInvoiceId);
        Assert.Equal(0, supplierCreateOutboxWriter.EnqueueCallsCount);
    }

    [Fact]
    public async Task RejectAsync_ShouldSetReviewAuditFields()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
        var supplierCreateOutboxWriter = new FakeSupplierCreateOutboxWriter();

        var service = new InvoiceReviewService(
            uploadedInvoiceStore,
            exactPostOutboxWriter,
            supplierCreateOutboxWriter);

        var invoice = new UploadedInvoiceRecord
        {
            InvoiceId = "invoice-9",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = "path",
            Status = InvoiceStatuses.NeedsReview,
            Message = InvoiceMessages.NeedsReview,
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = "hash",
            SupplierName = "Supplier",
            InvoiceNumber = "INV-009",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalAmount = 900.00m,
            Currency = "EUR",
            IsSupplierMatched = true,
            RequiresSupplierReview = true,
            SupplierMatchedBy = "Manual",
            InternalSupplierId = "internal-9",
            ExactSupplierId = "exact-9",
            SupplierMatchMessage = "Matched",
            CanCreateSupplier = false,
            SupplierAddressLine = "Address",
            SupplierPostcode = "12345",
            SupplierCity = "City",
            SupplierCountry = "NL",
            SupplierBankAccount = "IBAN",
            SupplierBicCode = "BIC",
            HasNewBankDetails = true,
            MatchReasons = new() { "Reason1" }
        };

        await uploadedInvoiceStore.SaveAsync(invoice, CancellationToken.None);

        var beforeReview = DateTime.UtcNow;
        await service.RejectAsync("invoice-9", CancellationToken.None);
        var afterReview = DateTime.UtcNow;

        var updatedInvoice = await uploadedInvoiceStore.GetByIdAsync("invoice-9", CancellationToken.None);

        Assert.NotNull(updatedInvoice);
        Assert.Equal(InvoiceStatuses.NeedsReview, updatedInvoice.Status);
        Assert.Equal(InvoiceMessages.ReviewRejected, updatedInvoice.Message);
        Assert.Equal(ReviewDecisions.Rejected, updatedInvoice.ReviewDecision);
        Assert.NotNull(updatedInvoice.ReviewedAtUtc);
        Assert.True(updatedInvoice.ReviewedAtUtc.Value >= beforeReview);
        Assert.True(updatedInvoice.ReviewedAtUtc.Value <= afterReview);
    }

    [Fact]
    public async Task RejectAsync_ShouldKeepNeedsReview_WhenInvoiceReviewIsRejected()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
        var supplierCreateOutboxWriter = new FakeSupplierCreateOutboxWriter();

        var service = new InvoiceReviewService(
            uploadedInvoiceStore,
            exactPostOutboxWriter,
            supplierCreateOutboxWriter);

        var invoice = new UploadedInvoiceRecord
        {
            InvoiceId = "invoice-5",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = "path",
            Status = InvoiceStatuses.NeedsReview,
            Message = InvoiceMessages.NeedsReview,
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = "hash",
            SupplierName = "Supplier",
            InvoiceNumber = "INV-005",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalAmount = 500.00m,
            Currency = "EUR",
            IsSupplierMatched = true,
            RequiresSupplierReview = true,
            SupplierMatchedBy = "Manual",
            InternalSupplierId = "internal-5",
            ExactSupplierId = "exact-5",
            SupplierMatchMessage = "Matched",
            CanCreateSupplier = false,
            SupplierAddressLine = "Address",
            SupplierPostcode = "12345",
            SupplierCity = "City",
            SupplierCountry = "NL",
            SupplierBankAccount = "IBAN",
            SupplierBicCode = "BIC",
            HasNewBankDetails = true,
            MatchReasons = new() { "Reason1" }
        };

        await uploadedInvoiceStore.SaveAsync(invoice, CancellationToken.None);

        await service.RejectAsync("invoice-5", CancellationToken.None);

        var updatedInvoice = await uploadedInvoiceStore.GetByIdAsync("invoice-5", CancellationToken.None);

        Assert.NotNull(updatedInvoice);
        Assert.Equal(InvoiceStatuses.NeedsReview, updatedInvoice.Status);
        Assert.Equal(InvoiceMessages.ReviewRejected, updatedInvoice.Message);
        Assert.Equal(1, updatedInvoice.MatchReasons.Count);
        Assert.Equal("Reason1", updatedInvoice.MatchReasons[0]);
        Assert.Equal("Supplier", updatedInvoice.SupplierName);
        Assert.Equal(0, exactPostOutboxWriter.EnqueueCallsCount);
        Assert.Equal(0, supplierCreateOutboxWriter.EnqueueCallsCount);
    }

    [Fact]
    public async Task RejectAsync_ShouldThrow_WhenInvoiceIsNotFound()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
        var supplierCreateOutboxWriter = new FakeSupplierCreateOutboxWriter();

        var service = new InvoiceReviewService(
            uploadedInvoiceStore,
            exactPostOutboxWriter,
            supplierCreateOutboxWriter);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.RejectAsync("missing-invoice", CancellationToken.None));
    }

    [Fact]
    public async Task RejectAsync_ShouldThrow_WhenInvoiceIsNotInNeedsReview()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
        var supplierCreateOutboxWriter = new FakeSupplierCreateOutboxWriter();

        var service = new InvoiceReviewService(
            uploadedInvoiceStore,
            exactPostOutboxWriter,
            supplierCreateOutboxWriter);

        var invoice = new UploadedInvoiceRecord
        {
            InvoiceId = "invoice-6",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = "path",
            Status = InvoiceStatuses.Parsed,
            Message = "Parsed",
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = "hash",
            SupplierName = "Supplier",
            InvoiceNumber = "INV-006",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalAmount = 600.00m,
            Currency = "EUR",
            IsSupplierMatched = false,
            RequiresSupplierReview = false,
            SupplierMatchedBy = null,
            InternalSupplierId = null,
            ExactSupplierId = null,
            SupplierMatchMessage = null,
            CanCreateSupplier = false,
            SupplierAddressLine = null,
            SupplierPostcode = null,
            SupplierCity = null,
            SupplierCountry = null,
            SupplierBankAccount = null,
            SupplierBicCode = null,
            HasNewBankDetails = false,
            MatchReasons = new()
        };

        await uploadedInvoiceStore.SaveAsync(invoice, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RejectAsync("invoice-6", CancellationToken.None));

        Assert.Contains("not in 'NeedsReview' status", exception.Message);
    }

    [Fact]
    public async Task RejectAsync_ShouldNotQueueAnyOutboxItems()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
        var supplierCreateOutboxWriter = new FakeSupplierCreateOutboxWriter();

        var service = new InvoiceReviewService(
            uploadedInvoiceStore,
            exactPostOutboxWriter,
            supplierCreateOutboxWriter);

        var invoice = new UploadedInvoiceRecord
        {
            InvoiceId = "invoice-7",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = "path",
            Status = InvoiceStatuses.NeedsReview,
            Message = InvoiceMessages.NeedsReview,
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = "hash",
            SupplierName = "Supplier",
            InvoiceNumber = "INV-007",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalAmount = 700.00m,
            Currency = "EUR",
            IsSupplierMatched = false,
            RequiresSupplierReview = true,
            SupplierMatchedBy = null,
            InternalSupplierId = null,
            ExactSupplierId = null,
            SupplierMatchMessage = null,
            CanCreateSupplier = true,
            SupplierAddressLine = "Address",
            SupplierPostcode = "12345",
            SupplierCity = "City",
            SupplierCountry = "NL",
            SupplierBankAccount = "IBAN",
            SupplierBicCode = "BIC",
            HasNewBankDetails = true,
            MatchReasons = new() { "Reason1" }
        };

        await uploadedInvoiceStore.SaveAsync(invoice, CancellationToken.None);

        await service.RejectAsync("invoice-7", CancellationToken.None);

        Assert.Equal(0, exactPostOutboxWriter.EnqueueCallsCount);
        Assert.Equal(0, supplierCreateOutboxWriter.EnqueueCallsCount);
    }

    [Fact]
    public async Task ApproveAsync_ShouldThrow_WhenInvoiceHasNoSafeNextStep()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
        var supplierCreateOutboxWriter = new FakeSupplierCreateOutboxWriter();

        var service = new InvoiceReviewService(
            uploadedInvoiceStore,
            exactPostOutboxWriter,
            supplierCreateOutboxWriter);

        var invoice = new UploadedInvoiceRecord
        {
            InvoiceId = "invoice-4",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = "path",
            Status = InvoiceStatuses.NeedsReview,
            Message = "Needs review",
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = "hash",
            SupplierName = "Supplier",
            InvoiceNumber = "INV-004",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalAmount = 400.00m,
            Currency = "EUR",
            IsSupplierMatched = false,
            RequiresSupplierReview = true,
            SupplierMatchedBy = null,
            InternalSupplierId = null,
            ExactSupplierId = null,
            SupplierMatchMessage = null,
            CanCreateSupplier = false,
            SupplierAddressLine = null,
            SupplierPostcode = null,
            SupplierCity = null,
            SupplierCountry = null,
            SupplierBankAccount = null,
            SupplierBicCode = null,
            HasNewBankDetails = true,
            MatchReasons = new() { "Reason1" }
        };

        await uploadedInvoiceStore.SaveAsync(invoice, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ApproveAsync("invoice-4", CancellationToken.None));

        Assert.Contains("no safe next step", exception.Message);
    }
}
