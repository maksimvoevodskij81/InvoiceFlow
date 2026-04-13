using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Invoices;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using System.Collections.Concurrent;

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