using InvoiceFlow.Api.Features.Exact;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using InvoiceFlow.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Api.Features.Suppliers.CreateSupplier;

public sealed class SupplierCreateWorker : BackgroundService
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
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Supplier create worker failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<InvoiceFlowDbContext>();
        var supplierCreator = scope.ServiceProvider.GetRequiredService<ISupplierCreator>();
        var exactPostOutboxWriter = scope.ServiceProvider.GetRequiredService<IExactPostOutboxWriter>();
        var uploadedInvoiceStore = scope.ServiceProvider.GetRequiredService<IUploadedInvoiceStore>();

        var pendingMessages = await dbContext.SupplierCreateOutbox
            .Where(x => x.Status == SupplierCreateOutboxStatuses.Pending)
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

                string exactSupplierId = await supplierCreator.CreateAsync(message.InvoiceId, cancellationToken);

                message.Status = SupplierCreateOutboxStatuses.Succeeded;
                message.CreatedExactSupplierId = exactSupplierId;
                message.LastError = null;

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
            }
        }
    }
}