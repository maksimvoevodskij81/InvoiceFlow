using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Invoices;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;

namespace InvoiceFlow.Api.Tests.Fakes;

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

    public Task<UploadedInvoiceRecord?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default)
    {
        var record = _records.Values.FirstOrDefault(x => string.Equals(x.FileHash, fileHash, StringComparison.Ordinal));

        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<UploadedInvoiceRecord>> QueryAsync(
        InvoiceListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        IEnumerable<UploadedInvoiceRecord> source = _records.Values;

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(x => x.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.ReviewDecision))
        {
            source = source.Where(x => x.ReviewDecision == query.ReviewDecision);
        }

        if (query.CanCreateSupplier.HasValue)
        {
            source = source.Where(x => x.CanCreateSupplier == query.CanCreateSupplier.Value);
        }

        var list = source
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();

        return Task.FromResult((IReadOnlyList<UploadedInvoiceRecord>)list);
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

    public async Task UpdateSupplierCreationResultAsync(
   string invoiceId,
   string exactSupplierId,
   CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(exactSupplierId);

        var existingRecord = await this.GetByIdAsync(invoiceId, cancellationToken);

        if (existingRecord is null)
        {
            throw new InvalidOperationException($"Uploaded invoice with id '{invoiceId}' was not found.");
        }

        existingRecord.IsSupplierMatched = true;
        existingRecord.RequiresSupplierReview = false;
        existingRecord.CanCreateSupplier = false;

        existingRecord.ExactSupplierId = exactSupplierId;
        existingRecord.SupplierMatchedBy = SupplierMatchSources.CreatedInExact;
        existingRecord.SupplierMatchMessage = InvoiceMessages.SupplierCreatedInExactSuccessfully;

        existingRecord.Status = InvoiceStatuses.ReadyToPost;
        existingRecord.Message = InvoiceMessages.ReadyToPost;

        await this.SaveAsync(existingRecord, cancellationToken);
    }
}