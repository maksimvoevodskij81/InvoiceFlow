using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Invoices;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using InvoiceFlow.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Api.Tests.Features.Invoices;

public sealed class UploadedInvoiceStoreQueryTests
{
    [Fact]
    public async Task QueryAsync_ShouldReturnAllInvoices_WhenNoFiltersProvided()
    {
        await using var dbContext = CreateDbContext();
        var store = new EfUploadedInvoiceStore(dbContext);

        await store.SaveAsync(CreateRecord("invoice-1", InvoiceStatuses.Parsed, ReviewDecisions.Approved, true), CancellationToken.None);
        await store.SaveAsync(CreateRecord("invoice-2", InvoiceStatuses.NeedsReview, ReviewDecisions.Rejected, false), CancellationToken.None);

        var results = await store.QueryAsync(new InvoiceListQuery(), CancellationToken.None);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task QueryAsync_ShouldFilterByStatus()
    {
        await using var dbContext = CreateDbContext();
        var store = new EfUploadedInvoiceStore(dbContext);

        await store.SaveAsync(CreateRecord("invoice-1", InvoiceStatuses.Parsed, ReviewDecisions.Approved, true), CancellationToken.None);
        await store.SaveAsync(CreateRecord("invoice-2", InvoiceStatuses.NeedsReview, ReviewDecisions.Rejected, false), CancellationToken.None);

        var results = await store.QueryAsync(new InvoiceListQuery { Status = InvoiceStatuses.Parsed }, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("invoice-1", results[0].InvoiceId);
    }

    [Fact]
    public async Task QueryAsync_ShouldFilterByReviewDecision()
    {
        await using var dbContext = CreateDbContext();
        var store = new EfUploadedInvoiceStore(dbContext);

        await store.SaveAsync(CreateRecord("invoice-1", InvoiceStatuses.Parsed, ReviewDecisions.Approved, true), CancellationToken.None);
        await store.SaveAsync(CreateRecord("invoice-2", InvoiceStatuses.Parsed, ReviewDecisions.Rejected, false), CancellationToken.None);

        var results = await store.QueryAsync(new InvoiceListQuery { ReviewDecision = ReviewDecisions.Rejected }, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("invoice-2", results[0].InvoiceId);
    }

    [Fact]
    public async Task QueryAsync_ShouldFilterByCanCreateSupplier()
    {
        await using var dbContext = CreateDbContext();
        var store = new EfUploadedInvoiceStore(dbContext);

        await store.SaveAsync(CreateRecord("invoice-1", InvoiceStatuses.Parsed, ReviewDecisions.Approved, true), CancellationToken.None);
        await store.SaveAsync(CreateRecord("invoice-2", InvoiceStatuses.Parsed, ReviewDecisions.Approved, false), CancellationToken.None);

        var results = await store.QueryAsync(new InvoiceListQuery { CanCreateSupplier = true }, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("invoice-1", results[0].InvoiceId);
    }

    [Fact]
    public async Task QueryAsync_ShouldApplyCombinedFilters()
    {
        await using var dbContext = CreateDbContext();
        var store = new EfUploadedInvoiceStore(dbContext);

        await store.SaveAsync(CreateRecord("invoice-1", InvoiceStatuses.Parsed, ReviewDecisions.Approved, true), CancellationToken.None);
        await store.SaveAsync(CreateRecord("invoice-2", InvoiceStatuses.Parsed, ReviewDecisions.Rejected, true), CancellationToken.None);
        await store.SaveAsync(CreateRecord("invoice-3", InvoiceStatuses.NeedsReview, ReviewDecisions.Rejected, true), CancellationToken.None);

        var results = await store.QueryAsync(new InvoiceListQuery
        {
            Status = InvoiceStatuses.Parsed,
            ReviewDecision = ReviewDecisions.Rejected,
            CanCreateSupplier = true
        }, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("invoice-2", results[0].InvoiceId);
    }

    private static InvoiceFlowDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InvoiceFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InvoiceFlowDbContext(options);
    }

    private static UploadedInvoiceRecord CreateRecord(
        string invoiceId,
        string status,
        string? reviewDecision,
        bool canCreateSupplier)
    {
        return new UploadedInvoiceRecord
        {
            InvoiceId = invoiceId,
            OriginalFileName = "invoice.pdf",
            StoredFilePath = Path.Combine("temp", "invoice.pdf"),
            Status = status,
            Message = "Message",
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = Guid.NewGuid().ToString(),
            SupplierName = "Demo Supplier",
            InvoiceNumber = "INV-001",
            InvoiceDate = new DateOnly(2026, 4, 1),
            TotalAmount = 123.45m,
            Currency = "EUR",
            IsSupplierMatched = true,
            RequiresSupplierReview = false,
            SupplierMatchedBy = SupplierMatchSources.BankAccount,
            InternalSupplierId = "internal",
            ExactSupplierId = "exact",
            SupplierMatchMessage = "Match",
            ExactPostingStatus = null,
            ExactDocumentId = null,
            PostedToExactAtUtc = null,
            ExactPostingError = null,
            ReviewedAtUtc = null,
            ReviewDecision = reviewDecision,
            ReviewComment = null,
            CanCreateSupplier = canCreateSupplier,
            SupplierAddressLine = null,
            SupplierPostcode = null,
            SupplierCity = null,
            SupplierCountry = null,
            SupplierBankAccount = null,
            SupplierBicCode = null,
            HasNewBankDetails = false,
            MatchReasons = new List<string>()
        };
    }
}
