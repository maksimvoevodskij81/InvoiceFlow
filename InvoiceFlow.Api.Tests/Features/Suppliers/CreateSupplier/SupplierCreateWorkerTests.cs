using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Exact;
using InvoiceFlow.Api.Features.Invoices;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using InvoiceFlow.Api.Features.Suppliers.CreateSupplier;
using InvoiceFlow.Api.Infrastructure.Background;
using InvoiceFlow.Api.Infrastructure.Persistence;
using InvoiceFlow.Api.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiceFlow.Api.Tests.Features.Suppliers.CreateSupplier;

public sealed class SupplierCreateWorkerTests
{
    [Fact]
    public async Task ProcessPendingMessagesAsync_ShouldCreateSupplier_UpdateInvoice_AndEnqueueExactPost()
    {
        var options = new DbContextOptionsBuilder<InvoiceFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InvoiceFlowDbContext(options);

        dbContext.SupplierCreateOutbox.Add(new SupplierCreateOutboxEntity
        {
            Id = Guid.NewGuid(),
            InvoiceId = "inv-1",
            Status = SupplierCreateOutboxStatuses.Pending,
            AttemptCount = 0,
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        await uploadedInvoiceStore.SaveAsync(new UploadedInvoiceRecord
        {
            InvoiceId = "inv-1",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = "temp/invoice.pdf",
            Status = InvoiceStatuses.Parsed,
            Message = InvoiceMessages.ParsedSuccessfully,
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = "hash-1",
            SupplierName = "Supplier A",
            InvoiceNumber = "INV-001",
            InvoiceDate = new DateOnly(2026, 4, 1),
            TotalAmount = 100m,
            Currency = "EUR",
            IsSupplierMatched = false,
            RequiresSupplierReview = false,
            CanCreateSupplier = true
        });

        var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
        var supplierCreator = new FakeSupplierCreator
        {
            ExactSupplierId = "exact-created-1"
        };

        var serviceProvider = new ServiceCollection()
            .AddSingleton(dbContext)
            .AddSingleton<IUploadedInvoiceStore>(uploadedInvoiceStore)
            .AddSingleton<IExactPostOutboxWriter>(exactPostOutboxWriter)
            .AddSingleton<ISupplierCreator>(supplierCreator)
            .BuildServiceProvider();

        var worker = new TestableSupplierCreateWorker(
            serviceProvider,
            NullLogger<SupplierCreateWorker>.Instance);

        await worker.RunOnceAsync(CancellationToken.None);

        var outboxMessage = await dbContext.SupplierCreateOutbox.SingleAsync(x => x.InvoiceId == "inv-1");
        var updatedRecord = await uploadedInvoiceStore.GetByIdAsync("inv-1", CancellationToken.None);

        Assert.Equal(SupplierCreateOutboxStatuses.Succeeded, outboxMessage.Status);
        Assert.Equal(1, outboxMessage.AttemptCount);
        Assert.Equal("exact-created-1", outboxMessage.CreatedExactSupplierId);
        Assert.Null(outboxMessage.LastError);

        Assert.NotNull(updatedRecord);
        Assert.True(updatedRecord!.IsSupplierMatched);
        Assert.False(updatedRecord.RequiresSupplierReview);
        Assert.False(updatedRecord.CanCreateSupplier);
        Assert.Equal("exact-created-1", updatedRecord.ExactSupplierId);
        Assert.Equal(SupplierMatchSources.CreatedInExact, updatedRecord.SupplierMatchedBy);
        Assert.Equal(InvoiceMessages.SupplierCreatedInExactSuccessfully, updatedRecord.SupplierMatchMessage);
        Assert.Equal(InvoiceStatuses.ReadyToPost, updatedRecord.Status);
        Assert.Equal(InvoiceMessages.ReadyToPost, updatedRecord.Message);

        Assert.Equal(1, exactPostOutboxWriter.EnqueueCallsCount);
        Assert.Equal("inv-1", exactPostOutboxWriter.LastEnqueuedInvoiceId);
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_ShouldMarkMessageFailed_WhenSupplierCreationThrows()
    {
        var options = new DbContextOptionsBuilder<InvoiceFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InvoiceFlowDbContext(options);

        dbContext.SupplierCreateOutbox.Add(new SupplierCreateOutboxEntity
        {
            Id = Guid.NewGuid(),
            InvoiceId = "inv-2",
            Status = SupplierCreateOutboxStatuses.Pending,
            AttemptCount = 0,
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        await uploadedInvoiceStore.SaveAsync(new UploadedInvoiceRecord
        {
            InvoiceId = "inv-2",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = "temp/invoice.pdf",
            Status = InvoiceStatuses.Parsed,
            Message = InvoiceMessages.ParsedSuccessfully,
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = "hash-2",
            SupplierName = "Supplier B",
            InvoiceNumber = "INV-002",
            InvoiceDate = new DateOnly(2026, 4, 1),
            TotalAmount = 200m,
            Currency = "EUR",
            IsSupplierMatched = false,
            RequiresSupplierReview = false,
            CanCreateSupplier = true
        });

        var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
        var supplierCreator = new ThrowingSupplierCreator("Exact create failed.");

        var serviceProvider = new ServiceCollection()
            .AddSingleton(dbContext)
            .AddSingleton<IUploadedInvoiceStore>(uploadedInvoiceStore)
            .AddSingleton<IExactPostOutboxWriter>(exactPostOutboxWriter)
            .AddSingleton<ISupplierCreator>(supplierCreator)
            .BuildServiceProvider();

        var worker = new TestableSupplierCreateWorker(
            serviceProvider,
            NullLogger<SupplierCreateWorker>.Instance);

        await worker.RunOnceAsync(CancellationToken.None);

        var outboxMessage = await dbContext.SupplierCreateOutbox.SingleAsync(x => x.InvoiceId == "inv-2");
        var updatedRecord = await uploadedInvoiceStore.GetByIdAsync("inv-2", CancellationToken.None);

        Assert.Equal(SupplierCreateOutboxStatuses.Failed, outboxMessage.Status);
        Assert.Equal(1, outboxMessage.AttemptCount);
        Assert.NotNull(outboxMessage.LastAttemptAtUtc);
        Assert.NotNull(outboxMessage.NextAttemptAtUtc);
        Assert.Equal("Exact create failed.", outboxMessage.LastError);
        Assert.Null(outboxMessage.CreatedExactSupplierId);

        Assert.NotNull(updatedRecord);
        Assert.False(updatedRecord!.IsSupplierMatched);
        Assert.True(updatedRecord.CanCreateSupplier);
        Assert.Null(updatedRecord.ExactSupplierId);

        Assert.Equal(0, exactPostOutboxWriter.EnqueueCallsCount);
        Assert.Null(exactPostOutboxWriter.LastEnqueuedInvoiceId);
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_ShouldDoNothing_WhenThereAreNoPendingMessages()
    {
        var options = new DbContextOptionsBuilder<InvoiceFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InvoiceFlowDbContext(options);

        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
        var supplierCreator = new FakeSupplierCreator
        {
            ExactSupplierId = "unused"
        };

        var serviceProvider = new ServiceCollection()
            .AddSingleton(dbContext)
            .AddSingleton<IUploadedInvoiceStore>(uploadedInvoiceStore)
            .AddSingleton<IExactPostOutboxWriter>(exactPostOutboxWriter)
            .AddSingleton<ISupplierCreator>(supplierCreator)
            .BuildServiceProvider();

        var worker = new TestableSupplierCreateWorker(
            serviceProvider,
            NullLogger<SupplierCreateWorker>.Instance);

        await worker.RunOnceAsync(CancellationToken.None);

        Assert.Equal(0, exactPostOutboxWriter.EnqueueCallsCount);
        Assert.Null(exactPostOutboxWriter.LastEnqueuedInvoiceId);
        Assert.Equal(0, await dbContext.SupplierCreateOutbox.CountAsync());
    }
}

file sealed class TestableSupplierCreateWorker : SupplierCreateWorker
{
    private readonly IServiceProvider _serviceProvider;

    public TestableSupplierCreateWorker(
        IServiceProvider serviceProvider,
        ILogger<SupplierCreateWorker> logger)
        : base(new TestServiceScopeFactory(serviceProvider), logger)
    {
        _serviceProvider = serviceProvider;
    }

    public Task RunOnceAsync(CancellationToken cancellationToken)
    {
        return this.ProcessPendingMessagesPublicAsync(cancellationToken);
    }
}

file sealed class TestServiceScopeFactory : IServiceScopeFactory
{
    private readonly IServiceProvider _serviceProvider;

    public TestServiceScopeFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IServiceScope CreateScope()
    {
        return new TestServiceScope(_serviceProvider);
    }
}

file sealed class TestServiceScope : IServiceScope
{
    public TestServiceScope(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    public void Dispose()
    {
    }
}

file sealed class FakeSupplierCreator : ISupplierCreator
{
    public string ExactSupplierId { get; set; } = string.Empty;

    public Task<string> CreateAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ExactSupplierId);
    }
}

file sealed class ThrowingSupplierCreator : ISupplierCreator
{
    private readonly string _message;

    public ThrowingSupplierCreator(string message)
    {
        _message = message;
    }

    public Task<string> CreateAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(_message);
    }
}