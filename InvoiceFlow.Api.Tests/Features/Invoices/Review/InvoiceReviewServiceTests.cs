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
