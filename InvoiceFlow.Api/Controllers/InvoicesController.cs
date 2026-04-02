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
    private readonly ISupplierMatcher _supplierMatcher;
    public InvoicesController(
        IInvoiceFolderReader invoiceFolderReader,
        IInvoiceParser invoiceParser,
        ISupplierMatcher supplierMatcher)
    {
        _invoiceFolderReader = invoiceFolderReader;
        _invoiceParser = invoiceParser;
        _supplierMatcher = supplierMatcher;
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
        var supplierMatchResult = await _supplierMatcher.MatchAsync(parseResult);
        var response = CreateImportInvoicesFromFolderResponse(file, parseResult, supplierMatchResult);

        return Ok(response);
    }

    private static ImportInvoicesFromFolderResponse CreateImportInvoicesFromFolderResponse(
    FolderInvoiceFile file,
    InvoiceParseResult parseResult,
    SupplierMatchResult supplierMatchResult)
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
            Currency = parseResult.Currency,
            IsSupplierMatched = supplierMatchResult.IsMatched,
            RequiresSupplierReview = supplierMatchResult.RequiresReview,
            SupplierMatchedBy = supplierMatchResult.MatchedBy,
            InternalSupplierId = supplierMatchResult.InternalSupplierId,
            ExactSupplierId = supplierMatchResult.ExactSupplierId,
            SupplierMatchMessage = supplierMatchResult.Message
        };
    }
}