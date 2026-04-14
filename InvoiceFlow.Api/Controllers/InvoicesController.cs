using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Invoices;
using InvoiceFlow.Api.Features.Invoices.GetInvoiceDetails;
using InvoiceFlow.Api.Features.Invoices.GetInvoiceStatus;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using InvoiceFlow.Api.Features.Suppliers.Matching;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.Api.Controllers;

[ApiController]
[Route("api/invoices")]
public sealed class InvoicesController : ControllerBase
{
    private readonly IInvoiceFolderReader _invoiceFolderReader;
    private readonly IInvoiceParser _invoiceParser;
    private readonly ISupplierMatcher _supplierMatcher;
    private readonly IInvoiceUploadService _invoiceUploadService;
    private readonly IUploadedInvoiceStore _uploadedInvoiceStore;
    private readonly InvoiceParseResultValidator _invoiceParseResultValidator;

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
        IInvoiceUploadService invoiceUploadService,
        IUploadedInvoiceStore uploadedInvoiceStore,
        InvoiceParseResultValidator invoiceParseResultValidator)
    {
        _invoiceFolderReader = invoiceFolderReader;
        _invoiceParser = invoiceParser;
        _supplierMatcher = supplierMatcher;
        _invoiceUploadService = invoiceUploadService;
        _uploadedInvoiceStore = uploadedInvoiceStore;
        _invoiceParseResultValidator = invoiceParseResultValidator;
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

        var response = await _invoiceUploadService.UploadAsync(request.File, cancellationToken);

        return Ok(response);
    }
    [HttpPost("import-from-folder")]
    [ProducesResponseType(typeof(ImportInvoicesFromFolderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImportInvoicesFromFolderResponse>> ImportFromFolder(
        [FromBody] ImportInvoicesFromFolderRequest request,
        CancellationToken cancellationToken)
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

        var parseResult = await _invoiceParser.ParseAsync(file, cancellationToken);

        List<string> missingFields = _invoiceParseResultValidator.Validate(parseResult);

        if (missingFields.Count > 0)
        {
            var invalidResponse = CreateInvalidImportInvoicesFromFolderResponse(
                file,
                parseResult,
                missingFields);

            return Ok(invalidResponse);
        }
        var supplierMatchResult = await _supplierMatcher.MatchAsync(parseResult, cancellationToken);
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
            Message = record.Message ?? string.Empty,
            SupplierName = record.SupplierName,
            InvoiceNumber = record.InvoiceNumber,
            InvoiceDate = record.InvoiceDate,
            TotalAmount = record.TotalAmount,
            Currency = record.Currency,
            IsSupplierMatched = record.IsSupplierMatched,
            RequiresSupplierReview = record.RequiresSupplierReview,
            SupplierMatchedBy = record.SupplierMatchedBy,
            InternalSupplierId = record.InternalSupplierId,
            ExactSupplierId = record.ExactSupplierId,
            SupplierMatchMessage = record.SupplierMatchMessage,
            ExactPostingStatus = record.ExactPostingStatus,
            ExactDocumentId = record.ExactDocumentId,
            PostedToExactAtUtc = record.PostedToExactAtUtc,
            ExactPostingError = record.ExactPostingError,
            CanCreateSupplier = record.CanCreateSupplier,
            HasNewBankDetails = record.HasNewBankDetails,
            MatchReasons = record.MatchReasons,
        };

        return Ok(response);
    }


    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GetInvoiceDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetInvoiceDetailsResponse>> GetById(
    string id,
    CancellationToken cancellationToken)
    {
        var record = await _uploadedInvoiceStore.GetByIdAsync(id, cancellationToken);

        if (record is null)
        {
            return NotFound();
        }

        var response = new GetInvoiceDetailsResponse
        {
            InvoiceId = record.InvoiceId,
            OriginalFileName = record.OriginalFileName,
            CreatedAtUtc = record.CreatedAtUtc,
            Status = record.Status,
            Message = record.Message ?? string.Empty,
            SupplierName = record.SupplierName,
            InvoiceNumber = record.InvoiceNumber,
            InvoiceDate = record.InvoiceDate,
            TotalAmount = record.TotalAmount,
            Currency = record.Currency,
            IsSupplierMatched = record.IsSupplierMatched,
            RequiresSupplierReview = record.RequiresSupplierReview,
            SupplierMatchedBy = record.SupplierMatchedBy,
            InternalSupplierId = record.InternalSupplierId,
            ExactSupplierId = record.ExactSupplierId,
            SupplierMatchMessage = record.SupplierMatchMessage,
            ExactPostingStatus = record.ExactPostingStatus,
            ExactDocumentId = record.ExactDocumentId,
            PostedToExactAtUtc = record.PostedToExactAtUtc,
            ExactPostingError = record.ExactPostingError,
            CanCreateSupplier = record.CanCreateSupplier,
            HasNewBankDetails = record.HasNewBankDetails,
            MatchReasons = record.MatchReasons,
        };

        return Ok(response);
    }

    private static ImportInvoicesFromFolderResponse CreateImportInvoicesFromFolderResponse(
    FolderInvoiceFile file,
    InvoiceParseResult parseResult,
    SupplierMatchResult supplierMatchResult)
    {
        var isReadyToPost = IsReadyToPost(supplierMatchResult);
        return new ImportInvoicesFromFolderResponse
        {
            FileName = file.FileName,
            FullPath = file.FullPath,
            ContentType = file.ContentType,
            Status = isReadyToPost
                ? InvoiceStatuses.ReadyToPost
                : InvoiceStatuses.Parsed,
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

    private static ImportInvoicesFromFolderResponse CreateInvalidImportInvoicesFromFolderResponse(
    FolderInvoiceFile file,
    InvoiceParseResult parseResult,
    List<string> missingFields)
    {
        return new ImportInvoicesFromFolderResponse
        {
            FileName = file.FileName,
            FullPath = file.FullPath,
            ContentType = file.ContentType,
            Status = InvoiceStatuses.Invalid,
            SupplierName = parseResult.SupplierName,
            InvoiceNumber = parseResult.InvoiceNumber,
            InvoiceDate = parseResult.InvoiceDate,
            TotalAmount = parseResult.TotalAmount,
            Currency = parseResult.Currency,
            IsSupplierMatched = false,
            RequiresSupplierReview = false,
            SupplierMatchedBy = null,
            InternalSupplierId = null,
            ExactSupplierId = null,
            SupplierMatchMessage = InvoiceMessages.MissingRequiredFields(missingFields)
        };
    }

    private static bool IsReadyToPost(SupplierMatchResult supplierMatchResult)
    {
        ArgumentNullException.ThrowIfNull(supplierMatchResult);

        return supplierMatchResult.IsMatched &&
               !supplierMatchResult.RequiresReview &&
               !string.IsNullOrWhiteSpace(supplierMatchResult.ExactSupplierId);
    }
}