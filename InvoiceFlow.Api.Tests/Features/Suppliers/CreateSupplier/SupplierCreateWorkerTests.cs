using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Exact;
using InvoiceFlow.Api.Features.Invoices;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using InvoiceFlow.Api.Features.Suppliers.CreateSupplier;
using InvoiceFlow.Api.Features.Suppliers.Idempotency;
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
            .AddSingleton<ISupplierMappingStore, FakeSupplierMappingStore>()
            .AddSingleton<IBankAccountMappingStore, FakeBankAccountMappingStore>()
            .AddSingleton<SupplierFingerprintBuilder>()
            .AddSingleton<BankAccountFingerprintBuilder>()
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
            .AddSingleton<ISupplierMappingStore, FakeSupplierMappingStore>()
            .AddSingleton<IBankAccountMappingStore, FakeBankAccountMappingStore>()
            .AddSingleton<SupplierFingerprintBuilder>()
            .AddSingleton<BankAccountFingerprintBuilder>()
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
            .AddSingleton<ISupplierMappingStore, FakeSupplierMappingStore>()
            .AddSingleton<IBankAccountMappingStore, FakeBankAccountMappingStore>()
            .AddSingleton<SupplierFingerprintBuilder>()
            .AddSingleton<BankAccountFingerprintBuilder>()
            .BuildServiceProvider();

        var worker = new TestableSupplierCreateWorker(
            serviceProvider,
            NullLogger<SupplierCreateWorker>.Instance);

        await worker.RunOnceAsync(CancellationToken.None);

        Assert.Equal(0, exactPostOutboxWriter.EnqueueCallsCount);
        Assert.Null(exactPostOutboxWriter.LastEnqueuedInvoiceId);
        Assert.Equal(0, await dbContext.SupplierCreateOutbox.CountAsync());
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_ShouldRetryFailedMessage_WhenNextAttemptAtUtcHasPassed()
    {
        var options = new DbContextOptionsBuilder<InvoiceFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InvoiceFlowDbContext(options);

        dbContext.SupplierCreateOutbox.Add(new SupplierCreateOutboxEntity
        {
            Id = Guid.NewGuid(),
            InvoiceId = "inv-retry-1",
            Status = SupplierCreateOutboxStatuses.Failed,
            AttemptCount = 1,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            LastAttemptAtUtc = DateTime.UtcNow.AddMinutes(-6),
            NextAttemptAtUtc = DateTime.UtcNow.AddMinutes(-1),
            LastError = "Previous failure"
        });

        await dbContext.SaveChangesAsync();

        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        await uploadedInvoiceStore.SaveAsync(new UploadedInvoiceRecord
        {
            InvoiceId = "inv-retry-1",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = "temp/invoice.pdf",
            Status = InvoiceStatuses.Parsed,
            Message = InvoiceMessages.ParsedSuccessfully,
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = "hash-retry-1",
            SupplierName = "Retry Supplier",
            InvoiceNumber = "INV-RETRY-001",
            InvoiceDate = new DateOnly(2026, 4, 1),
            TotalAmount = 300m,
            Currency = "EUR",
            SupplierAddressLine = "Retry street 1",
            SupplierPostcode = "1234AB",
            SupplierCity = "Amsterdam",
            SupplierCountry = "NL",
            SupplierBankAccount = "NL91ABNA0417164300",
            SupplierBicCode = "ABNANL2A",
            IsSupplierMatched = false,
            RequiresSupplierReview = false,
            CanCreateSupplier = true
        });

        var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
        var supplierCreator = new FakeSupplierCreator
        {
            ExactSupplierId = "exact-retry-1"
        };

        var serviceProvider = new ServiceCollection()
            .AddSingleton(dbContext)
            .AddSingleton<IUploadedInvoiceStore>(uploadedInvoiceStore)
            .AddSingleton<IExactPostOutboxWriter>(exactPostOutboxWriter)
            .AddSingleton<ISupplierCreator>(supplierCreator)
            .AddSingleton<ISupplierMappingStore, FakeSupplierMappingStore>()
            .AddSingleton<IBankAccountMappingStore, FakeBankAccountMappingStore>()
            .AddSingleton<SupplierFingerprintBuilder>()
            .AddSingleton<BankAccountFingerprintBuilder>()
            .BuildServiceProvider();

        var worker = new TestableSupplierCreateWorker(
            serviceProvider,
            NullLogger<SupplierCreateWorker>.Instance);

        await worker.RunOnceAsync(CancellationToken.None);

        var outboxMessage = await dbContext.SupplierCreateOutbox.SingleAsync(x => x.InvoiceId == "inv-retry-1");
        var updatedRecord = await uploadedInvoiceStore.GetByIdAsync("inv-retry-1", CancellationToken.None);

        Assert.Equal(SupplierCreateOutboxStatuses.Succeeded, outboxMessage.Status);
        Assert.Equal(2, outboxMessage.AttemptCount);
        Assert.Equal("exact-retry-1", outboxMessage.CreatedExactSupplierId);
        Assert.Null(outboxMessage.LastError);
        Assert.Null(outboxMessage.NextAttemptAtUtc);

        Assert.NotNull(updatedRecord);
        Assert.True(updatedRecord!.IsSupplierMatched);
        Assert.False(updatedRecord.RequiresSupplierReview);
        Assert.False(updatedRecord.CanCreateSupplier);
        Assert.Equal("exact-retry-1", updatedRecord.ExactSupplierId);
        Assert.Equal(InvoiceStatuses.ReadyToPost, updatedRecord.Status);

        Assert.Equal(1, exactPostOutboxWriter.EnqueueCallsCount);
        Assert.Equal("inv-retry-1", exactPostOutboxWriter.LastEnqueuedInvoiceId);
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_ShouldReuseExistingSupplierMapping_WhenSupplierFingerprintExists()
    {
        var options = new DbContextOptionsBuilder<InvoiceFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InvoiceFlowDbContext(options);

        dbContext.SupplierCreateOutbox.Add(new SupplierCreateOutboxEntity
        {
            Id = Guid.NewGuid(),
            InvoiceId = "inv-map-1",
            Status = SupplierCreateOutboxStatuses.Pending,
            AttemptCount = 0,
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        await uploadedInvoiceStore.SaveAsync(new UploadedInvoiceRecord
        {
            InvoiceId = "inv-map-1",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = "temp/invoice.pdf",
            Status = InvoiceStatuses.Parsed,
            Message = InvoiceMessages.ParsedSuccessfully,
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = "hash-map-1",
            SupplierName = "Mapped Supplier",
            InvoiceNumber = "INV-MAP-001",
            InvoiceDate = new DateOnly(2026, 4, 1),
            TotalAmount = 150m,
            Currency = "EUR",
            SupplierAddressLine = "Street 1",
            SupplierPostcode = "1234AB",
            SupplierCity = "Amsterdam",
            SupplierCountry = "NL",
            SupplierBankAccount = "NL91ABNA0417164300",
            SupplierBicCode = "ABNANL2A",
            IsSupplierMatched = false,
            RequiresSupplierReview = false,
            CanCreateSupplier = true
        });

        var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
        var supplierCreator = new CountingSupplierCreator
        {
            ExactSupplierId = "should-not-be-used"
        };

        var supplierMappingStore = new FakeSupplierMappingStore();
        var bankAccountMappingStore = new FakeBankAccountMappingStore();
        var supplierFingerprintBuilder = new SupplierFingerprintBuilder();
        var bankAccountFingerprintBuilder = new BankAccountFingerprintBuilder();

        var supplierFingerprint = supplierFingerprintBuilder.Build(new InvoiceParseResult
        {
            SupplierName = "Mapped Supplier",
            SupplierPostcode = "1234AB",
            SupplierCountry = "NL"
        });

        await supplierMappingStore.SaveAsync(
            supplierFingerprint,
            "exact-existing-1",
            CancellationToken.None);

        var serviceProvider = new ServiceCollection()
            .AddSingleton(dbContext)
            .AddSingleton<IUploadedInvoiceStore>(uploadedInvoiceStore)
            .AddSingleton<IExactPostOutboxWriter>(exactPostOutboxWriter)
            .AddSingleton<ISupplierCreator>(supplierCreator)
            .AddSingleton<ISupplierMappingStore>(supplierMappingStore)
            .AddSingleton<IBankAccountMappingStore>(bankAccountMappingStore)
            .AddSingleton(supplierFingerprintBuilder)
            .AddSingleton(bankAccountFingerprintBuilder)
            .BuildServiceProvider();

        var worker = new TestableSupplierCreateWorker(
            serviceProvider,
            NullLogger<SupplierCreateWorker>.Instance);

        await worker.RunOnceAsync(CancellationToken.None);

        var outboxMessage = await dbContext.SupplierCreateOutbox.SingleAsync(x => x.InvoiceId == "inv-map-1");
        var updatedRecord = await uploadedInvoiceStore.GetByIdAsync("inv-map-1", CancellationToken.None);

        Assert.Equal(0, supplierCreator.CreateCallsCount);
        Assert.Equal(SupplierCreateOutboxStatuses.Succeeded, outboxMessage.Status);
        Assert.Equal("exact-existing-1", outboxMessage.CreatedExactSupplierId);

        Assert.NotNull(updatedRecord);
        Assert.True(updatedRecord!.IsSupplierMatched);
        Assert.False(updatedRecord.RequiresSupplierReview);
        Assert.False(updatedRecord.CanCreateSupplier);
        Assert.Equal("exact-existing-1", updatedRecord.ExactSupplierId);
        Assert.Equal(InvoiceStatuses.ReadyToPost, updatedRecord.Status);

        Assert.Equal(1, exactPostOutboxWriter.EnqueueCallsCount);
        Assert.Equal("inv-map-1", exactPostOutboxWriter.LastEnqueuedInvoiceId);

        var bankFingerprint = bankAccountFingerprintBuilder.Build("NL91ABNA0417164300");
        var bankOwner = await bankAccountMappingStore.FindExactSupplierIdAsync(bankFingerprint, CancellationToken.None);

        Assert.Equal("exact-existing-1", bankOwner);
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_ShouldFail_WhenBankAccountBelongsToAnotherSupplier()
    {
        var options = new DbContextOptionsBuilder<InvoiceFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new InvoiceFlowDbContext(options);

        dbContext.SupplierCreateOutbox.Add(new SupplierCreateOutboxEntity
        {
            Id = Guid.NewGuid(),
            InvoiceId = "inv-conflict-1",
            Status = SupplierCreateOutboxStatuses.Pending,
            AttemptCount = 0,
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var uploadedInvoiceStore = new FakeUploadedInvoiceStore();
        await uploadedInvoiceStore.SaveAsync(new UploadedInvoiceRecord
        {
            InvoiceId = "inv-conflict-1",
            OriginalFileName = "invoice.pdf",
            StoredFilePath = "temp/invoice.pdf",
            Status = InvoiceStatuses.Parsed,
            Message = InvoiceMessages.ParsedSuccessfully,
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = "hash-conflict-1",
            SupplierName = "Conflict Supplier",
            InvoiceNumber = "INV-CONFLICT-001",
            InvoiceDate = new DateOnly(2026, 4, 1),
            TotalAmount = 175m,
            Currency = "EUR",
            SupplierAddressLine = "Street 2",
            SupplierPostcode = "9999ZZ",
            SupplierCity = "Rotterdam",
            SupplierCountry = "NL",
            SupplierBankAccount = "NL11TEST0123456789",
            SupplierBicCode = "TESTNL2A",
            IsSupplierMatched = false,
            RequiresSupplierReview = false,
            CanCreateSupplier = true
        });

        var exactPostOutboxWriter = new FakeExactPostOutboxWriter();
        var supplierCreator = new CountingSupplierCreator
        {
            ExactSupplierId = "should-not-be-used"
        };

        var supplierMappingStore = new FakeSupplierMappingStore();
        var bankAccountMappingStore = new FakeBankAccountMappingStore();
        var supplierFingerprintBuilder = new SupplierFingerprintBuilder();
        var bankAccountFingerprintBuilder = new BankAccountFingerprintBuilder();

        var supplierFingerprint = supplierFingerprintBuilder.Build(new InvoiceParseResult
        {
            SupplierName = "Conflict Supplier",
            SupplierPostcode = "9999ZZ",
            SupplierCountry = "NL"
        });

        await supplierMappingStore.SaveAsync(
            supplierFingerprint,
            "exact-supplier-a",
            CancellationToken.None);

        var bankFingerprint = bankAccountFingerprintBuilder.Build("NL11TEST0123456789");

        await bankAccountMappingStore.SaveAsync(
            bankFingerprint,
            "exact-supplier-b",
            CancellationToken.None);

        var serviceProvider = new ServiceCollection()
            .AddSingleton(dbContext)
            .AddSingleton<IUploadedInvoiceStore>(uploadedInvoiceStore)
            .AddSingleton<IExactPostOutboxWriter>(exactPostOutboxWriter)
            .AddSingleton<ISupplierCreator>(supplierCreator)
            .AddSingleton<ISupplierMappingStore>(supplierMappingStore)
            .AddSingleton<IBankAccountMappingStore>(bankAccountMappingStore)
            .AddSingleton(supplierFingerprintBuilder)
            .AddSingleton(bankAccountFingerprintBuilder)
            .BuildServiceProvider();

        var worker = new TestableSupplierCreateWorker(
            serviceProvider,
            NullLogger<SupplierCreateWorker>.Instance);

        await worker.RunOnceAsync(CancellationToken.None);

        var outboxMessage = await dbContext.SupplierCreateOutbox.SingleAsync(x => x.InvoiceId == "inv-conflict-1");
        var updatedRecord = await uploadedInvoiceStore.GetByIdAsync("inv-conflict-1", CancellationToken.None);

        Assert.Equal(0, supplierCreator.CreateCallsCount);
        Assert.Equal(SupplierCreateOutboxStatuses.Failed, outboxMessage.Status);
        Assert.Equal("Bank account is already linked to another supplier.", outboxMessage.LastError);
        Assert.NotNull(outboxMessage.NextAttemptAtUtc);

        Assert.NotNull(updatedRecord);
        Assert.False(updatedRecord!.IsSupplierMatched);
        Assert.True(updatedRecord.CanCreateSupplier);
        Assert.Null(updatedRecord.ExactSupplierId);

        Assert.Equal(0, exactPostOutboxWriter.EnqueueCallsCount);
        Assert.Null(exactPostOutboxWriter.LastEnqueuedInvoiceId);
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

    public Task<string> CreateAsync(SupplierCreateRequest request, CancellationToken cancellationToken = default)
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

    public Task<string> CreateAsync(SupplierCreateRequest request, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(_message);
    }
}

file sealed class FakeSupplierMappingStore : ISupplierMappingStore
{
    private readonly Dictionary<string, string> _mappings = new(StringComparer.Ordinal);

    public Task<string?> FindExactSupplierIdAsync(string fingerprint, CancellationToken cancellationToken = default)
    {
        _mappings.TryGetValue(fingerprint, out string? exactSupplierId);
        return Task.FromResult(exactSupplierId);
    }

    public Task SaveAsync(
        string fingerprint,
        string exactSupplierId,
        CancellationToken cancellationToken = default)
    {
        _mappings.TryAdd(fingerprint, exactSupplierId);
        return Task.CompletedTask;
    }
}

file sealed class FakeBankAccountMappingStore : IBankAccountMappingStore
{
    private readonly Dictionary<string, string> _mappings = new(StringComparer.Ordinal);

    public Task<string?> FindExactSupplierIdAsync(string fingerprint, CancellationToken cancellationToken = default)
    {
        _mappings.TryGetValue(fingerprint, out string? exactSupplierId);
        return Task.FromResult(exactSupplierId);
    }

    public Task SaveAsync(
        string fingerprint,
        string exactSupplierId,
        CancellationToken cancellationToken = default)
    {
        _mappings.TryAdd(fingerprint, exactSupplierId);
        return Task.CompletedTask;
    }
}

file sealed class CountingSupplierCreator : ISupplierCreator
{
    public int CreateCallsCount { get; private set; }
    public string ExactSupplierId { get; set; } = string.Empty;

    public Task<string> CreateAsync(SupplierCreateRequest request, CancellationToken cancellationToken = default)
    {
        CreateCallsCount++;
        return Task.FromResult(ExactSupplierId);
    }
}
