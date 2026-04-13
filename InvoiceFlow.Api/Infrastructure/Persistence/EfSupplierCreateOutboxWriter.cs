using InvoiceFlow.Api.Features.Suppliers.CreateSupplier;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Api.Infrastructure.Persistence;

public sealed class EfSupplierCreateOutboxWriter : ISupplierCreateOutboxWriter
{
    private readonly InvoiceFlowDbContext _dbContext;

    public EfSupplierCreateOutboxWriter(InvoiceFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task EnqueueAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceId);

        var existingMessage = await _dbContext.SupplierCreateOutbox
            .SingleOrDefaultAsync(x => x.InvoiceId == invoiceId, cancellationToken);

        if (existingMessage is not null)
        {
            return;
        }

        var message = new SupplierCreateOutboxEntity
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            Status = SupplierCreateOutboxStatuses.Pending,
            AttemptCount = 0,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.SupplierCreateOutbox.Add(message);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RequeueAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceId);

        var existingMessage = await _dbContext.SupplierCreateOutbox
            .SingleOrDefaultAsync(x => x.InvoiceId == invoiceId, cancellationToken);

        if (existingMessage is null)
        {
            var message = new SupplierCreateOutboxEntity
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoiceId,
                Status = SupplierCreateOutboxStatuses.Pending,
                AttemptCount = 0,
                CreatedAtUtc = DateTime.UtcNow,
                LastAttemptAtUtc = null,
                NextAttemptAtUtc = null,
                CreatedExactSupplierId = null,
                LastError = null
            };

            _dbContext.SupplierCreateOutbox.Add(message);
        }
        else
        {
            existingMessage.Status = SupplierCreateOutboxStatuses.Pending;
            existingMessage.AttemptCount = 0;
            existingMessage.LastAttemptAtUtc = null;
            existingMessage.NextAttemptAtUtc = null;
            existingMessage.CreatedExactSupplierId = null;
            existingMessage.LastError = null;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}