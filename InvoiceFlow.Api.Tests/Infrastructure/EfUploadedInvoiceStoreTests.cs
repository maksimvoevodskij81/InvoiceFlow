using InvoiceFlow.Api.Contracts;
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
            SupplierMatchedBy = "BankAccount",
            InternalSupplierId = "internal-supplier-001",
            ExactSupplierId = "exact-supplier-001",
            SupplierMatchMessage = "Supplier matched successfully."
        };
    }
}