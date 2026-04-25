using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Exact;
using InvoiceFlow.Api.Features.Invoices;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using InvoiceFlow.Api.Features.Suppliers.CreateSupplier;

namespace InvoiceFlow.Api.Features.Invoices.Review;

public sealed class InvoiceReviewService : IInvoiceReviewService
{
    private readonly IUploadedInvoiceStore _uploadedInvoiceStore;
    private readonly IExactPostOutboxWriter _exactPostOutboxWriter;
    private readonly ISupplierCreateOutboxWriter _supplierCreateOutboxWriter;

    public InvoiceReviewService(
        IUploadedInvoiceStore uploadedInvoiceStore,
        IExactPostOutboxWriter exactPostOutboxWriter,
        ISupplierCreateOutboxWriter supplierCreateOutboxWriter)
    {
        _uploadedInvoiceStore = uploadedInvoiceStore;
        _exactPostOutboxWriter = exactPostOutboxWriter;
        _supplierCreateOutboxWriter = supplierCreateOutboxWriter;
    }

    public async Task ApproveAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceId);

        var invoice = await _uploadedInvoiceStore.GetByIdAsync(invoiceId, cancellationToken);

        if (invoice is null)
        {
            throw new KeyNotFoundException($"Invoice with id '{invoiceId}' was not found.");
        }

        if (invoice.Status != InvoiceStatuses.NeedsReview)
        {
            throw new InvalidOperationException($"Invoice '{invoiceId}' is not in '{InvoiceStatuses.NeedsReview}' status.");
        }

        invoice.RequiresSupplierReview = false;
        invoice.HasNewBankDetails = false;
        invoice.MatchReasons = new();

        invoice.ReviewedAtUtc = DateTime.UtcNow;
        invoice.ReviewDecision = ReviewDecisions.Approved;

        if (!string.IsNullOrWhiteSpace(invoice.ExactSupplierId))
        {
            invoice.Status = InvoiceStatuses.ReadyToPost;
            invoice.Message = InvoiceMessages.ReadyToPost;

            await _uploadedInvoiceStore.SaveAsync(invoice, cancellationToken);
            await _exactPostOutboxWriter.EnqueueAsync(invoiceId, cancellationToken);
        }
        else if (invoice.CanCreateSupplier)
        {
            invoice.Status = InvoiceStatuses.Parsed;
            invoice.Message = InvoiceMessages.ParsedSuccessfully;

            await _uploadedInvoiceStore.SaveAsync(invoice, cancellationToken);
            await _supplierCreateOutboxWriter.EnqueueAsync(invoiceId, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException($"Invoice '{invoiceId}' has no safe next step.");
        }
    }

    public async Task RejectAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceId);

        var invoice = await _uploadedInvoiceStore.GetByIdAsync(invoiceId, cancellationToken);

        if (invoice is null)
        {
            throw new KeyNotFoundException($"Invoice with id '{invoiceId}' was not found.");
        }

        if (invoice.Status != InvoiceStatuses.NeedsReview)
        {
            throw new InvalidOperationException($"Invoice '{invoiceId}' is not in '{InvoiceStatuses.NeedsReview}' status.");
        }

        invoice.Message = InvoiceMessages.ReviewRejected;
        invoice.ReviewedAtUtc = DateTime.UtcNow;
        invoice.ReviewDecision = ReviewDecisions.Rejected;

        await _uploadedInvoiceStore.SaveAsync(invoice, cancellationToken);
    }
}
