using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Invoices.GetInvoiceStatus;
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
    private readonly IUploadedInvoiceFileStore _uploadedInvoiceFileStore;
    private readonly IUploadedInvoiceStore _uploadedInvoiceStore;
    private const long MaxUploadFileSizeInBytes = 10 * 1024 * 1024;
    private static readonly string[] AllowedUploadExtensions =
    {
    ".pdf",
    ".jpg",
    ".jpeg",
    ".png",
    ".tif",
    ".tiff"
};
    public InvoicesController(
        IInvoiceFolderReader invoiceFolderReader,
        IInvoiceParser invoiceParser,
        ISupplierMatcher supplierMatcher,
        IUploadedInvoiceFileStore uploadedInvoiceFileStore,
        IUploadedInvoiceStore uploadedInvoiceStore)
    {
        _invoiceFolderReader = invoiceFolderReader;
        _invoiceParser = invoiceParser;
        _supplierMatcher = supplierMatcher;
        _uploadedInvoiceFileStore = uploadedInvoiceFileStore;
        _uploadedInvoiceStore = uploadedInvoiceStore;
    }


    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadInvoiceAcceptedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadInvoiceAcceptedResponse>> Upload(
    [FromForm] UploadInvoiceRequest request,
    CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest("File is required.");
        }

        var fileExtension = Path.GetExtension(request.File.FileName);

        if (!AllowedUploadExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest("Only PDF, JPG, JPEG, PNG, TIF, and TIFF files are allowed.");
        }

        if (request.File.Length > MaxUploadFileSizeInBytes)
        {
            return BadRequest("File size must not exceed 10 MB.");
        }

        var invoiceId = Guid.NewGuid().ToString();
        var storedFilePath = await _uploadedInvoiceFileStore.SaveAsync(request.File, cancellationToken);

        var record = new UploadedInvoiceRecord
        {
            InvoiceId = invoiceId,
            OriginalFileName = request.File.FileName,
            StoredFilePath = storedFilePath,
            Status = InvoiceStatuses.Processing,
            Message = "Invoice upload received.",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _uploadedInvoiceStore.SaveAsync(record, cancellationToken);

        var response = new UploadInvoiceAcceptedResponse
        {
            InvoiceId = invoiceId,
            Status = record.Status,
            Message = record.Message
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

    [HttpGet("{id}/status")]
    [ProducesResponseType(typeof(GetInvoiceStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetInvoiceStatusResponse>> GetStatus(
    string id,
    CancellationToken cancellationToken)
    {
        var record = await _uploadedInvoiceStore.GetByIdAsync(id, cancellationToken);

        if (record is null)
        {
            return NotFound();
        }

        var response = new GetInvoiceStatusResponse
        {
            InvoiceId = record.InvoiceId,
            Status = record.Status,
            Message = record.Message ?? string.Empty
        };

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