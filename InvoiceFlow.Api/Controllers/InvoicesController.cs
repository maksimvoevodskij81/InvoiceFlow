using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using Microsoft.AspNetCore.Mvc;


namespace InvoiceFlow.Api.Controllers;

[ApiController]
[Route("api/invoices")]
public sealed class InvoicesController : ControllerBase
{
    private readonly IInvoiceFolderReader _invoiceFolderReader;

    public InvoicesController(IInvoiceFolderReader invoiceFolderReader)
    {
        _invoiceFolderReader = invoiceFolderReader;
    }

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

    [HttpPost("import-from-folder")]
    [ProducesResponseType(typeof(ImportInvoicesFromFolderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ImportInvoicesFromFolderResponse> ImportFromFolder([FromBody] ImportInvoicesFromFolderRequest request)
    {
        var file = _invoiceFolderReader.TakeNext(request.FolderPath);

        if (file is null)
        {
            return NotFound();
        }

        var response = new ImportInvoicesFromFolderResponse
        {
            FileName = file.FileName,
            FullPath = file.FullPath,
            ContentType = file.ContentType,
            Status = InvoiceStatuses.Uploaded
        };

        return Ok(response);
    }
}
