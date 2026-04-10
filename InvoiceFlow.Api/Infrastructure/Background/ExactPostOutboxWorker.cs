using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Exact;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using InvoiceFlow.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Api.Infrastructure.Background;

public sealed class ExactPostOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExactPostOutboxWorker> _logger;

    public ExactPostOutboxWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<ExactPostOutboxWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Exact outbox worker failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<InvoiceFlowDbContext>();
        var uploadedInvoiceStore = scope.ServiceProvider.GetRequiredService<IUploadedInvoiceStore>();
        var exactInvoicePostingService = scope.ServiceProvider.GetRequiredService<IExactInvoicePostingService>();

        var utcNow = DateTime.UtcNow;

        var messages = await dbContext.ExactPostOutbox
            .Where(x =>
                x.Status == ExactPostOutboxStatuses.Pending ||
                (x.Status == ExactPostOutboxStatuses.Failed &&
                 (x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= utcNow)))
            .OrderBy(x => x.CreatedAtUtc)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.Status = ExactPostOutboxStatuses.Processing;
            message.AttemptCount++;
            message.LastAttemptAtUtc = utcNow;
            message.LastError = null;

            await dbContext.SaveChangesAsync(cancellationToken);
            var invoiceToUpdate = await uploadedInvoiceStore.GetByIdAsync(message.InvoiceId, cancellationToken);

            try
            {
                var invoice = await uploadedInvoiceStore.GetByIdAsync(message.InvoiceId, cancellationToken);

                if (invoice is null)
                {
                    message.Status = ExactPostOutboxStatuses.Failed;
                    message.LastError = "Uploaded invoice record not found.";
                    message.NextAttemptAtUtc = utcNow.AddMinutes(5);

                    await dbContext.SaveChangesAsync(cancellationToken);
                    continue;
                }

                var result = await exactInvoicePostingService.PostAsync(invoice, cancellationToken);

                if (result.Success)
                {
                    message.Status = ExactPostOutboxStatuses.Posted;
                    message.ExternalDocumentId = result.ExternalDocumentId;
                    message.NextAttemptAtUtc = null;
                    message.LastError = null;

                    if (invoiceToUpdate is not null)
                    {
                        invoiceToUpdate.ExactPostingStatus = ExactPostingStatuses.Posted;
                        invoiceToUpdate.ExactDocumentId = result.ExternalDocumentId;
                        invoiceToUpdate.PostedToExactAtUtc = utcNow;
                        invoiceToUpdate.ExactPostingError = null;

                        await uploadedInvoiceStore.SaveAsync(invoiceToUpdate, cancellationToken);
                    }
                }
                else
                {
                    message.Status = ExactPostOutboxStatuses.Failed;
                    message.LastError = result.ErrorMessage ?? "Exact posting failed.";
                    message.NextAttemptAtUtc = utcNow.AddMinutes(GetRetryDelayMinutes(message.AttemptCount));

                    if (invoiceToUpdate is not null)
                    {
                        invoiceToUpdate.ExactPostingStatus = ExactPostingStatuses.Failed;
                        invoiceToUpdate.ExactPostingError = result.ErrorMessage ?? "Exact posting failed.";

                        await uploadedInvoiceStore.SaveAsync(invoiceToUpdate, cancellationToken);
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to post invoice {InvoiceId} to Exact.", message.InvoiceId);

                message.Status = ExactPostOutboxStatuses.Failed;
                message.LastError = exception.Message;
                message.NextAttemptAtUtc = utcNow.AddMinutes(GetRetryDelayMinutes(message.AttemptCount));

                if (invoiceToUpdate is not null)
                {
                    invoiceToUpdate.ExactPostingStatus = ExactPostingStatuses.Failed;
                    invoiceToUpdate.ExactPostingError = exception.Message;

                    await uploadedInvoiceStore.SaveAsync(invoiceToUpdate, cancellationToken);
                }
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private static int GetRetryDelayMinutes(int attemptCount)
    {
        return Math.Min(30, Math.Max(5, attemptCount * 5));
    }
}