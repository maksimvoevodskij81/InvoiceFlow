namespace InvoiceFlow.Api.Features.Invoices.UploadInvoice;

public interface IUploadedInvoiceStore
{
    Task SaveAsync(UploadedInvoiceRecord record, CancellationToken cancellationToken = default);

    Task<UploadedInvoiceRecord?> GetByIdAsync(string invoiceId, CancellationToken cancellationToken = default);
    Task<UploadedInvoiceRecord?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(
        string invoiceId,
        string status,
        string? message,
        CancellationToken cancellationToken = default);
}
