using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Invoices;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using InvoiceFlow.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Api.Tests.Infrastructure;

public sealed class EfUploadedInvoiceStoreTests
{
    [Fact]
    public async Task SaveAsync_ThenGetByIdAsync_ShouldPersistRecord()
    {
        await using var dbContext = CreateDbContext();
        var store = new EfUploadedInvoiceStore(dbContext);

        var record = CreateRecord(
            invoiceId: "invoice-1",
            fileHash: "hash-1",
            status: InvoiceStatuses.Processing,
            message: "Upload received.");

        await store.SaveAsync(record, CancellationToken.None);

        var savedRecord = await store.GetByIdAsync("invoice-1", CancellationToken.None);

        Assert.NotNull(savedRecord);
        Assert.Equal("invoice-1", savedRecord.InvoiceId);
        Assert.Equal("hash-1", savedRecord.FileHash);
        Assert.Equal(InvoiceStatuses.Processing, savedRecord.Status);
        Assert.Equal("Upload received.", savedRecord.Message);
    }

    [Fact]
    public async Task GetByFileHashAsync_ShouldReturnMatchingRecord()
    {
        await using var dbContext = CreateDbContext();
        var store = new EfUploadedInvoiceStore(dbContext);

        await store.SaveAsync(CreateRecord("invoice-1", "hash-abc", InvoiceStatuses.Parsed, "Parsed"), CancellationToken.None);

        var savedRecord = await store.GetByFileHashAsync("hash-abc", CancellationToken.None);

        Assert.NotNull(savedRecord);
        Assert.Equal("invoice-1", savedRecord.InvoiceId);
        Assert.Equal("hash-abc", savedRecord.FileHash);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldUpdateStatusAndMessage()
    {
        await using var dbContext = CreateDbContext();
        var store = new EfUploadedInvoiceStore(dbContext);

        await store.SaveAsync(CreateRecord("invoice-1", "hash-1", InvoiceStatuses.Processing, "Upload received."), CancellationToken.None);

        await store.UpdateStatusAsync("invoice-1", InvoiceStatuses.Failed, "Parsing failed.", CancellationToken.None);

        var savedRecord = await store.GetByIdAsync("invoice-1", CancellationToken.None);

        Assert.NotNull(savedRecord);
        Assert.Equal(InvoiceStatuses.Failed, savedRecord.Status);
        Assert.Equal("Parsing failed.", savedRecord.Message);
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistSupplierCreationFields()
    {
        await using var dbContext = CreateDbContext();
        var store = new EfUploadedInvoiceStore(dbContext);

        var record = new UploadedInvoiceRecord
        {
            InvoiceId = "invoice-supplier-test",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = Path.Combine("temp", "invoice.pdf"),
            Status = InvoiceStatuses.NeedsReview,
            Message = InvoiceMessages.NeedsReview,
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = "hash-supplier",
            SupplierName = "New Supplier Inc",
            InvoiceNumber = "INV-002",
            InvoiceDate = new DateOnly(2026, 4, 15),
            TotalAmount = 500.00m,
            Currency = "EUR",
            IsSupplierMatched = false,
            RequiresSupplierReview = true,
            SupplierMatchedBy = null,
            InternalSupplierId = null,
            ExactSupplierId = null,
            SupplierMatchMessage = null,
            ExactPostingStatus = null,
            ExactDocumentId = null,
            PostedToExactAtUtc = null,
            ExactPostingError = null,
            CanCreateSupplier = true,
            SupplierAddressLine = "123 Business Street",
            SupplierPostcode = "12345",
            SupplierCity = "Amsterdam",
            SupplierCountry = "NL",
            SupplierBankAccount = "NL91ABNA0417164300",
            SupplierBicCode = "ABNANL2A",
            HasNewBankDetails = true,
            MatchReasons = new List<string>
        {
            "Bank account match",
            "Name similarity"
        }
        };

        await store.SaveAsync(record, CancellationToken.None);

        var savedRecord = await store.GetByIdAsync("invoice-supplier-test", CancellationToken.None);

        Assert.NotNull(savedRecord);
        Assert.Equal("invoice-supplier-test", savedRecord.InvoiceId);
        Assert.Equal("invoice.pdf", savedRecord.OriginalFileName);
        Assert.Equal(InvoiceStatuses.NeedsReview, savedRecord.Status);
        Assert.Equal(InvoiceMessages.NeedsReview, savedRecord.Message);
        Assert.Equal("New Supplier Inc", savedRecord.SupplierName);
        Assert.Equal("INV-002", savedRecord.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 4, 15), savedRecord.InvoiceDate);
        Assert.Equal(500.00m, savedRecord.TotalAmount);
        Assert.Equal("EUR", savedRecord.Currency);
        Assert.False(savedRecord.IsSupplierMatched);
        Assert.True(savedRecord.RequiresSupplierReview);
        Assert.True(savedRecord.CanCreateSupplier);
        Assert.Equal("123 Business Street", savedRecord.SupplierAddressLine);
        Assert.Equal("12345", savedRecord.SupplierPostcode);
        Assert.Equal("Amsterdam", savedRecord.SupplierCity);
        Assert.Equal("NL", savedRecord.SupplierCountry);
        Assert.Equal("NL91ABNA0417164300", savedRecord.SupplierBankAccount);
        Assert.Equal("ABNANL2A", savedRecord.SupplierBicCode);
        Assert.True(savedRecord.HasNewBankDetails);
        Assert.NotNull(savedRecord.MatchReasons);
        Assert.Equal(2, savedRecord.MatchReasons.Count);
        Assert.Contains("Bank account match", savedRecord.MatchReasons);
        Assert.Contains("Name similarity", savedRecord.MatchReasons);
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
        string fileHash,
        string status,
        string message)
    {
        return new UploadedInvoiceRecord
        {
            InvoiceId = invoiceId,
            OriginalFileName = "invoice.pdf",
            StoredFilePath = Path.Combine("temp", "invoice.pdf"),
            Status = status,
            Message = message,
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = fileHash,
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
            ExactPostingStatus = null,
            ExactDocumentId = null,
            PostedToExactAtUtc = null,
            ExactPostingError = null,
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
    }
}