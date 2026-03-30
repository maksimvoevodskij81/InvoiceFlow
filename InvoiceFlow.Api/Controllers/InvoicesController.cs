using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Api.Controllers;

[ApiController]
[Route("api/invoices")]
public sealed class InvoicesController : ControllerBase
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadInvoiceResponse), StatusCodes.Status200OK)]
    public ActionResult<UploadInvoiceResponse> Upload([FromForm] UploadInvoiceRequest request)
    {
        var response = new UploadInvoiceResponse
        {
            InvoiceId = Guid.NewGuid(),
            FileName = request.File.FileName,
            Status = InvoiceStatuses.Uploaded
        };

        return Ok(response);
    }
}