using InvoiceFlow.Api.Features.Suppliers.Idempotency;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Api.Infrastructure.Persistence;

public sealed class EfBankAccountMappingStore : IBankAccountMappingStore
{
    private readonly InvoiceFlowDbContext _dbContext;

    public EfBankAccountMappingStore(InvoiceFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string?> FindExactSupplierIdAsync(string fingerprint, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fingerprint);

        return await _dbContext.BankAccountMappings
            .Where(x => x.Fingerprint == fingerprint)
            .Select(x => x.ExactSupplierId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAsync(
        string fingerprint,
        string exactSupplierId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(exactSupplierId);

        var existing = await _dbContext.BankAccountMappings
            .SingleOrDefaultAsync(x => x.Fingerprint == fingerprint, cancellationToken);

        if (existing is not null)
        {
            return;
        }

        _dbContext.BankAccountMappings.Add(new BankAccountMappingEntity
        {
            Id = Guid.NewGuid(),
            Fingerprint = fingerprint,
            ExactSupplierId = exactSupplierId,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}