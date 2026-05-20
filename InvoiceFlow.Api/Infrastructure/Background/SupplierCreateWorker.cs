using InvoiceFlow.Api.Features.Exact;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using InvoiceFlow.Api.Features.Suppliers.Idempotency;
using InvoiceFlow.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Api.Features.Suppliers.CreateSupplier;

public class SupplierCreateWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<SupplierCreateWorker> _logger;

    public SupplierCreateWorker(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<SupplierCreateWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesPublicAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Supplier create worker failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    protected internal async Task ProcessPendingMessagesPublicAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<InvoiceFlowDbContext>();
        var supplierCreator = scope.ServiceProvider.GetRequiredService<ISupplierCreator>();
        var exactPostOutboxWriter = scope.ServiceProvider.GetRequiredService<IExactPostOutboxWriter>();
        var uploadedInvoiceStore = scope.ServiceProvider.GetRequiredService<IUploadedInvoiceStore>();
        var supplierMappingStore = scope.ServiceProvider.GetRequiredService<ISupplierMappingStore>();
        var bankAccountMappingStore = scope.ServiceProvider.GetRequiredService<IBankAccountMappingStore>();
        var supplierFingerprintBuilder = scope.ServiceProvider.GetRequiredService<SupplierFingerprintBuilder>();
        var bankAccountFingerprintBuilder = scope.ServiceProvider.GetRequiredService<BankAccountFingerprintBuilder>();

        var pendingMessages = await dbContext.SupplierCreateOutbox
            .Where(x =>
                (x.Status == SupplierCreateOutboxStatuses.Pending ||
                 x.Status == SupplierCreateOutboxStatuses.Failed) &&
                (x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= DateTime.UtcNow))
            .OrderBy(x => x.CreatedAtUtc)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var message in pendingMessages)
        {
            try
            {
                message.Status = SupplierCreateOutboxStatuses.Processing;
                message.LastAttemptAtUtc = DateTime.UtcNow;
                message.AttemptCount++;

                await dbContext.SaveChangesAsync(cancellationToken);

                var invoice = await uploadedInvoiceStore.GetByIdAsync(message.InvoiceId, cancellationToken);

                if (invoice is null)
                {
                    throw new InvalidOperationException($"Invoice '{message.InvoiceId}' was not found.");
                }

                var parseLikeModel = new InvoiceParseResult
                {
                    SupplierName = invoice.SupplierName ?? string.Empty,
                    SupplierAddressLine = invoice.SupplierAddressLine,
                    SupplierPostcode = invoice.SupplierPostcode,
                    SupplierCountry = invoice.SupplierCountry,
                    SupplierBankAccount = invoice.SupplierBankAccount
                };

                string supplierFingerprint = supplierFingerprintBuilder.Build(parseLikeModel);
                string bankFingerprint = bankAccountFingerprintBuilder.Build(invoice.SupplierBankAccount);

                string? supplierExactId = await supplierMappingStore.FindExactSupplierIdAsync(
                    supplierFingerprint,
                    cancellationToken);

                string? bankExactId = string.IsNullOrWhiteSpace(bankFingerprint)
                    ? null
                    : await bankAccountMappingStore.FindExactSupplierIdAsync(
                        bankFingerprint,
                        cancellationToken);

                if (!string.IsNullOrWhiteSpace(bankExactId) &&
                    !string.IsNullOrWhiteSpace(supplierExactId) &&
                    !string.Equals(bankExactId, supplierExactId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Bank account is already linked to another supplier.");
                }

                string exactSupplierId;

                if (!string.IsNullOrWhiteSpace(supplierExactId))
                {
                    exactSupplierId = supplierExactId;
                }
                else if (!string.IsNullOrWhiteSpace(bankExactId))
                {
                    throw new InvalidOperationException(
                        "Bank account already exists without a matching supplier fingerprint. Manual review is required.");
                }
                else
                {
                    var request = new SupplierCreateRequest
                    {
                        Name = invoice.SupplierName ?? string.Empty,
                        AddressLine = invoice.SupplierAddressLine ?? string.Empty,
                        Postcode = invoice.SupplierPostcode ?? string.Empty,
                        City = invoice.SupplierCity ?? string.Empty,
                        Country = invoice.SupplierCountry ?? string.Empty,
                        BankAccount = invoice.SupplierBankAccount ?? string.Empty,
                        BicCode = invoice.SupplierBicCode
                    };

                    exactSupplierId = await supplierCreator.CreateAsync(request, cancellationToken);

                    await supplierMappingStore.SaveAsync(
                        supplierFingerprint,
                        exactSupplierId,
                        cancellationToken);

                    if (!string.IsNullOrWhiteSpace(invoice.SupplierName) && !string.IsNullOrWhiteSpace(invoice.SupplierPostcode))
                    {
                        await supplierMappingStore.SaveAsync(
                            supplierFingerprintBuilder.BuildNamePostcode(parseLikeModel),
                            exactSupplierId,
                            cancellationToken);
                    }

                    if (!string.IsNullOrWhiteSpace(invoice.SupplierName) && !string.IsNullOrWhiteSpace(invoice.SupplierAddressLine) && !string.IsNullOrWhiteSpace(invoice.SupplierPostcode))
                    {
                        await supplierMappingStore.SaveAsync(
                            supplierFingerprintBuilder.BuildNameAddressPostcode(parseLikeModel),
                            exactSupplierId,
                            cancellationToken);
                    }
                }

                if (!string.IsNullOrWhiteSpace(bankFingerprint))
                {
                    string? existingBankOwner = await bankAccountMappingStore.FindExactSupplierIdAsync(
                        bankFingerprint,
                        cancellationToken);

                    if (string.IsNullOrWhiteSpace(existingBankOwner))
                    {
                        await bankAccountMappingStore.SaveAsync(
                            bankFingerprint,
                            exactSupplierId,
                            cancellationToken);
                    }
                    else if (!string.Equals(existingBankOwner, exactSupplierId, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Bank account is already linked to another supplier.");
                    }
                }

                message.Status = SupplierCreateOutboxStatuses.Succeeded;
                message.CreatedExactSupplierId = exactSupplierId;
                message.LastError = null;
                message.NextAttemptAtUtc = null;

                await dbContext.SaveChangesAsync(cancellationToken);

                await uploadedInvoiceStore.UpdateSupplierCreationResultAsync(
                    message.InvoiceId,
                    exactSupplierId,
                    cancellationToken);

                await exactPostOutboxWriter.EnqueueAsync(message.InvoiceId, cancellationToken);
            }
            catch (Exception exception)
            {
                message.Status = SupplierCreateOutboxStatuses.Failed;
                message.LastError = exception.Message;
                message.NextAttemptAtUtc = DateTime.UtcNow.AddMinutes(5);

                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogError(
                    exception,
                    "Failed to create supplier for invoice {InvoiceId}.",
                    message.InvoiceId);
            }
        }
    }
}