using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Exact;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Api.Infrastructure.Persistence;

public sealed class EfExactPostOutboxWriter : IExactPostOutboxWriter
{
    private readonly InvoiceFlowDbContext _dbContext;

    public EfExactPostOutboxWriter(InvoiceFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task EnqueueAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceId);

        var existingMessage = await _dbContext.ExactPostOutbox
            .SingleOrDefaultAsync(x => x.InvoiceId == invoiceId, cancellationToken);

        if (existingMessage is not null)
        {
            return;
        }

        var message = new ExactPostOutboxEntity
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            Status = ExactPostOutboxStatuses.Pending,
            AttemptCount = 0,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.ExactPostOutbox.Add(message);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}