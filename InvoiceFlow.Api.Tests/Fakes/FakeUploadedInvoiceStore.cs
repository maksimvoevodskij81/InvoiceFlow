using InvoiceFlow.Api.Features.Invoices.UploadInvoice;

public sealed class FakeUploadedInvoiceStore : IUploadedInvoiceStore
{
    private readonly Dictionary<string, UploadedInvoiceRecord> _records = new();

    public Task SaveAsync(UploadedInvoiceRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        _records[record.InvoiceId] = record;

        return Task.CompletedTask;
    }

    public Task<UploadedInvoiceRecord?> GetByIdAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        _records.TryGetValue(invoiceId, out var record);

        return Task.FromResult(record);
    }

    public Task UpdateStatusAsync(
        string invoiceId,
        string status,
        string? message,
        CancellationToken cancellationToken = default)
    {
        if (_records.TryGetValue(invoiceId, out var record))
        {
            record.Status = status;
            record.Message = message;
        }

        return Task.CompletedTask;
    }
}