using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Exact;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Api.Controllers;

[ApiController]
[Route("api/invoices")]
public sealed class InvoiceExactPostingController : ControllerBase
{
    private readonly IUploadedInvoiceStore _uploadedInvoiceStore;
    private readonly IExactPostOutboxWriter _exactPostOutboxWriter;

    public InvoiceExactPostingController(
        IUploadedInvoiceStore uploadedInvoiceStore,
        IExactPostOutboxWriter exactPostOutboxWriter)
    {
        _uploadedInvoiceStore = uploadedInvoiceStore;
        _exactPostOutboxWriter = exactPostOutboxWriter;
    }

    [HttpPost("{id}/retry-exact-post")]
    [ProducesResponseType(typeof(RetryExactPostResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RetryExactPostResponse>> RetryExactPost(
        string id,
        CancellationToken cancellationToken)
    {
        var invoice = await _uploadedInvoiceStore.GetByIdAsync(id, cancellationToken);

        if (invoice is null)
        {
            return NotFound();
        }

        if (!invoice.IsSupplierMatched ||
            invoice.RequiresSupplierReview ||
            string.IsNullOrWhiteSpace(invoice.ExactSupplierId))
        {
            return BadRequest("Invoice is not ready for Exact posting.");
        }

        if (!string.Equals(invoice.ExactPostingStatus, ExactPostingStatuses.Failed, StringComparison.Ordinal))
        {
            return BadRequest("Only failed Exact postings can be retried.");
        }

        invoice.ExactPostingStatus = ExactPostingStatuses.Queued;
        invoice.ExactPostingError = null;
        invoice.ExactDocumentId = null;
        invoice.PostedToExactAtUtc = null;

        await _uploadedInvoiceStore.SaveAsync(invoice, cancellationToken);
        await _exactPostOutboxWriter.RequeueAsync(invoice.InvoiceId, cancellationToken);

        var response = new RetryExactPostResponse
        {
            InvoiceId = invoice.InvoiceId,
            ExactPostingStatus = ExactPostingStatuses.Queued,
            Message = "Exact posting retry queued."
        };

        return Ok(response);
    }
}