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

        await service.ApproveAsync("invoice-1", null, null, cancellationToken: CancellationToken.None);

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

        await service.ApproveAsync("invoice-2", null, null, cancellationToken: CancellationToken.None);

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
            () => service.ApproveAsync("invoice-3", null, null, cancellationToken: CancellationToken.None));

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
        await service.ApproveAsync("invoice-8", null, null, cancellationToken: CancellationToken.None);
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
    public async Task ApproveAsync_ShouldSetReviewComment()
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
            InvoiceId = "invoice-10",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = "path",
            Status = InvoiceStatuses.NeedsReview,
            Message = InvoiceMessages.NeedsReview,
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = "hash",
            SupplierName = "Supplier",
            InvoiceNumber = "INV-010",
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalAmount = 1000.00m,
            Currency = "EUR",
            IsSupplierMatched = true,
            RequiresSupplierReview = true,
            SupplierMatchedBy = "Manual",
            InternalSupplierId = "internal-10",
            ExactSupplierId = "exact-10",
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

        var reviewComment = "Approved after manual review.";
        await service.ApproveAsync("invoice-10", reviewComment, null, cancellationToken: CancellationToken.None);

        var updatedInvoice = await uploadedInvoiceStore.GetByIdAsync("invoice-10", CancellationToken.None);

        Assert.NotNull(updatedInvoice);
        Assert.Equal(ReviewDecisions.Approved, updatedInvoice.ReviewDecision);
        Assert.Equal(reviewComment, updatedInvoice.ReviewComment);
    }

    [Fact]
    public async Task RejectAsync_ShouldSetReviewComment()
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
        var reviewComment = "Rejected due to duplicate supplier entry.";
        await service.RejectAsync("invoice-9", reviewComment, cancellationToken: CancellationToken.None);
        var afterReview = DateTime.UtcNow;

        var updatedInvoice = await uploadedInvoiceStore.GetByIdAsync("invoice-9", CancellationToken.None);

        Assert.NotNull(updatedInvoice);
        Assert.Equal(InvoiceStatuses.NeedsReview, updatedInvoice.Status);
        Assert.Equal(InvoiceMessages.ReviewRejected, updatedInvoice.Message);
        Assert.Equal(ReviewDecisions.Rejected, updatedInvoice.ReviewDecision);
        Assert.Equal(reviewComment, updatedInvoice.ReviewComment);
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

        await service.RejectAsync("invoice-5", null, cancellationToken: CancellationToken.None);

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
            () => service.RejectAsync("missing-invoice", null, cancellationToken: CancellationToken.None));
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
            () => service.RejectAsync("invoice-6", null, cancellationToken: CancellationToken.None));

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

        await service.RejectAsync("invoice-7", null, cancellationToken: CancellationToken.None);

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
            () => service.ApproveAsync("invoice-4", null, null, cancellationToken: CancellationToken.None));

        Assert.Contains("no safe next step", exception.Message);
    }

    [Fact]
    public async Task ApproveAsync_WithAcceptedFields_OverwritesMainColumns()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var service = new InvoiceReviewService(uploadedInvoiceStore, new FakeExactPostOutboxWriter(), new FakeSupplierCreateOutboxWriter());

        var invoice = BuildNeedsReviewInvoice("invoice-acc-1");
        await uploadedInvoiceStore.SaveAsync(invoice, CancellationToken.None);

        var accepted = new AcceptedInvoiceFields
        {
            SupplierName  = "Corrected BV",
            InvoiceNumber = "INV-CORRECTED",
            InvoiceDate   = new DateOnly(2026, 3, 1),
            TotalAmount   = 999.99m,
            Currency      = "GBP"
        };

        await service.ApproveAsync("invoice-acc-1", null, accepted, cancellationToken: CancellationToken.None);

        var updated = await uploadedInvoiceStore.GetByIdAsync("invoice-acc-1", CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal("Corrected BV",   updated.SupplierName);
        Assert.Equal("INV-CORRECTED",  updated.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 3, 1), updated.InvoiceDate);
        Assert.Equal(999.99m,          updated.TotalAmount);
        Assert.Equal("GBP",            updated.Currency);
    }

    [Fact]
    public async Task ApproveAsync_WithAcceptedFields_StoresAcceptedColumns()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var service = new InvoiceReviewService(uploadedInvoiceStore, new FakeExactPostOutboxWriter(), new FakeSupplierCreateOutboxWriter());

        var invoice = BuildNeedsReviewInvoice("invoice-acc-2");
        await uploadedInvoiceStore.SaveAsync(invoice, CancellationToken.None);

        var accepted = new AcceptedInvoiceFields
        {
            SupplierName  = "Corrected BV",
            InvoiceNumber = "INV-CORRECTED",
            InvoiceDate   = new DateOnly(2026, 3, 1),
            TotalAmount   = 999.99m,
            Currency      = "GBP"
        };

        await service.ApproveAsync("invoice-acc-2", null, accepted, cancellationToken: CancellationToken.None);

        var updated = await uploadedInvoiceStore.GetByIdAsync("invoice-acc-2", CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal("Corrected BV",            updated.AcceptedSupplierName);
        Assert.Equal("INV-CORRECTED",           updated.AcceptedInvoiceNumber);
        Assert.Equal(new DateOnly(2026, 3, 1),  updated.AcceptedInvoiceDate);
        Assert.Equal(999.99m,                   updated.AcceptedTotalAmount);
        Assert.Equal("GBP",                     updated.AcceptedCurrency);
    }

    [Fact]
    public async Task ApproveAsync_WithNullAcceptedFields_LeavesMainColumnsUnchanged()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var service = new InvoiceReviewService(uploadedInvoiceStore, new FakeExactPostOutboxWriter(), new FakeSupplierCreateOutboxWriter());

        var invoice = BuildNeedsReviewInvoice("invoice-acc-3");
        await uploadedInvoiceStore.SaveAsync(invoice, CancellationToken.None);

        await service.ApproveAsync("invoice-acc-3", null, acceptedFields: null, cancellationToken: CancellationToken.None);

        var updated = await uploadedInvoiceStore.GetByIdAsync("invoice-acc-3", CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal("Original Supplier", updated.SupplierName);
        Assert.Equal("INV-ORIG",          updated.InvoiceNumber);
        Assert.Equal(100.00m,             updated.TotalAmount);
        Assert.Equal("EUR",               updated.Currency);
        Assert.Null(updated.AcceptedSupplierName);
        Assert.Null(updated.AcceptedInvoiceNumber);
    }

    [Fact]
    public async Task ApproveAsync_WithPartialAcceptedFields_OnlyOverwritesNonNullFields()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var service = new InvoiceReviewService(uploadedInvoiceStore, new FakeExactPostOutboxWriter(), new FakeSupplierCreateOutboxWriter());

        var invoice = BuildNeedsReviewInvoice("invoice-acc-4");
        await uploadedInvoiceStore.SaveAsync(invoice, CancellationToken.None);

        var accepted = new AcceptedInvoiceFields
        {
            TotalAmount = 500.00m
        };

        await service.ApproveAsync("invoice-acc-4", null, accepted, cancellationToken: CancellationToken.None);

        var updated = await uploadedInvoiceStore.GetByIdAsync("invoice-acc-4", CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal("Original Supplier", updated.SupplierName);
        Assert.Equal("INV-ORIG",          updated.InvoiceNumber);
        Assert.Equal(500.00m,             updated.TotalAmount);
        Assert.Equal("EUR",               updated.Currency);
        Assert.Null(updated.AcceptedSupplierName);
        Assert.Equal(500.00m,             updated.AcceptedTotalAmount);
        Assert.Null(updated.AcceptedCurrency);
    }

    [Fact]
    public async Task ApproveAsync_ShouldStoreReviewedBy_WhenReviewerIsProvided()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var service = new InvoiceReviewService(uploadedInvoiceStore, new FakeExactPostOutboxWriter(), new FakeSupplierCreateOutboxWriter());
        var invoice = BuildNeedsReviewInvoice("invoice-reviewer-1");
        await uploadedInvoiceStore.SaveAsync(invoice, CancellationToken.None);

        await service.ApproveAsync("invoice-reviewer-1", null, null, reviewedBy: "reviewer@example.com", CancellationToken.None);

        var updated = await uploadedInvoiceStore.GetByIdAsync("invoice-reviewer-1", CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal("reviewer@example.com", updated.ReviewedBy);
    }

    [Fact]
    public async Task ApproveAsync_ShouldStoreNullReviewedBy_WhenNoReviewerProvided()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var service = new InvoiceReviewService(uploadedInvoiceStore, new FakeExactPostOutboxWriter(), new FakeSupplierCreateOutboxWriter());
        var invoice = BuildNeedsReviewInvoice("invoice-reviewer-2");
        await uploadedInvoiceStore.SaveAsync(invoice, CancellationToken.None);

        await service.ApproveAsync("invoice-reviewer-2", null, null, reviewedBy: null, CancellationToken.None);

        var updated = await uploadedInvoiceStore.GetByIdAsync("invoice-reviewer-2", CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Null(updated.ReviewedBy);
    }

    [Fact]
    public async Task RejectAsync_ShouldStoreReviewedBy_WhenReviewerIsProvided()
    {
        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var service = new InvoiceReviewService(uploadedInvoiceStore, new FakeExactPostOutboxWriter(), new FakeSupplierCreateOutboxWriter());
        var invoice = BuildNeedsReviewInvoice("invoice-reviewer-3");
        await uploadedInvoiceStore.SaveAsync(invoice, CancellationToken.None);

        await service.RejectAsync("invoice-reviewer-3", null, reviewedBy: "reviewer@example.com", CancellationToken.None);

        var updated = await uploadedInvoiceStore.GetByIdAsync("invoice-reviewer-3", CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal("reviewer@example.com", updated.ReviewedBy);
    }

    private static UploadedInvoiceRecord BuildNeedsReviewInvoice(string invoiceId) => new()
    {
        InvoiceId        = invoiceId,
        OriginalFileName = "invoice.pdf",
        StoredFilePath   = "path",
        Status           = InvoiceStatuses.NeedsReview,
        Message          = "Needs review",
        CreatedAtUtc     = DateTime.UtcNow,
        FileHash         = "hash",
        SupplierName     = "Original Supplier",
        InvoiceNumber    = "INV-ORIG",
        InvoiceDate      = new DateOnly(2026, 1, 1),
        TotalAmount      = 100.00m,
        Currency         = "EUR",
        ExactSupplierId  = "exact-1",
        CanCreateSupplier = false
    };
}
