using System.Collections.Concurrent;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;

namespace InvoiceFlow.Api.Infrastructure;

public sealed class InMemoryUploadedInvoiceStore : IUploadedInvoiceStore
{
    private readonly ConcurrentDictionary<string, UploadedInvoiceRecord> _records = new();

    public Task SaveAsync(UploadedInvoiceRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        _records[record.InvoiceId] = record;

        return Task.CompletedTask;
    }

    public Task<UploadedInvoiceRecord?> GetByIdAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceId);

        _records.TryGetValue(invoiceId, out var record);

        return Task.FromResult(record);
    }

    public Task<UploadedInvoiceRecord?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileHash);

        var record = _records.Values.FirstOrDefault(x => string.Equals(x.FileHash, fileHash, StringComparison.Ordinal));

        return Task.FromResult(record);
    }

    public Task UpdateStatusAsync(
        string invoiceId,
        string status,
        string? message,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        if (_records.TryGetValue(invoiceId, out var record))
        {
            record.Status = status;
            record.Message = message;
        }

        return Task.CompletedTask;
    }
}