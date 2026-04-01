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
    private readonly IInvoiceParser _invoiceParser;

    public InvoicesController(
        IInvoiceFolderReader invoiceFolderReader,
        IInvoiceParser invoiceParser)
    {
        _invoiceFolderReader = invoiceFolderReader;
        _invoiceParser = invoiceParser;
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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImportInvoicesFromFolderResponse>> ImportFromFolder([FromBody] ImportInvoicesFromFolderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FolderPath))
        {
            return BadRequest("FolderPath is required.");
        }

        var file = _invoiceFolderReader.TakeNext(request.FolderPath);

        if (file is null)
        {
            return NotFound();
        }

        var parseResult = await _invoiceParser.ParseAsync(file);

        var response = CreateImportInvoicesFromFolderResponse(file, parseResult);

        return Ok(response);
    }

    private static ImportInvoicesFromFolderResponse CreateImportInvoicesFromFolderResponse(
        FolderInvoiceFile file,
        InvoiceParseResult parseResult)
    {
        return new ImportInvoicesFromFolderResponse
        {
            FileName = file.FileName,
            FullPath = file.FullPath,
            ContentType = file.ContentType,
            Status = InvoiceStatuses.Parsed,
            SupplierName = parseResult.SupplierName,
            InvoiceNumber = parseResult.InvoiceNumber,
            InvoiceDate = parseResult.InvoiceDate,
            TotalAmount = parseResult.TotalAmount,
            Currency = parseResult.Currency
        };
    }
}