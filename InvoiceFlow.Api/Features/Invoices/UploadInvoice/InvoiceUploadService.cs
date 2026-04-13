using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Exact;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using System.Security.Cryptography;

namespace InvoiceFlow.Api.Features.Invoices.UploadInvoice;

public sealed class InvoiceUploadService : IInvoiceUploadService
{
    private readonly IInvoiceParser _invoiceParser;
    private readonly ISupplierMatcher _supplierMatcher;
    private readonly IUploadedInvoiceFileStore _uploadedInvoiceFileStore;
    private readonly IUploadedInvoiceStore _uploadedInvoiceStore;
    private readonly IExactPostOutboxWriter _exactPostOutboxWriter;
    private readonly InvoiceParseResultValidator _invoiceParseResultValidator;
    public InvoiceUploadService(
        IInvoiceParser invoiceParser,
        ISupplierMatcher supplierMatcher,
        IUploadedInvoiceFileStore uploadedInvoiceFileStore,
        IUploadedInvoiceStore uploadedInvoiceStore,
        IExactPostOutboxWriter exactPostOutboxWriter,
        InvoiceParseResultValidator invoiceParseResultValidator)
    {
        _invoiceParser = invoiceParser;
        _supplierMatcher = supplierMatcher;
        _uploadedInvoiceFileStore = uploadedInvoiceFileStore;
        _uploadedInvoiceStore = uploadedInvoiceStore;
        _exactPostOutboxWriter = exactPostOutboxWriter;
        _invoiceParseResultValidator = invoiceParseResultValidator;
    }

    public async Task<UploadInvoiceAcceptedResponse> UploadAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        var fileHash = await ComputeFileHashAsync(file, cancellationToken);
        var existingRecord = await _uploadedInvoiceStore.GetByFileHashAsync(fileHash, cancellationToken);

        if (existingRecord is not null)
        {
            return new UploadInvoiceAcceptedResponse
            {
                InvoiceId = existingRecord.InvoiceId,
                Status = InvoiceStatuses.Duplicate,
                Message = InvoiceMessages.DuplicateUploadDetected,
                MissingFields = new List<string>()
            };
        }

        var invoiceId = Guid.NewGuid().ToString();
        var storedFilePath = await _uploadedInvoiceFileStore.SaveAsync(file, cancellationToken);

        var record = CreateProcessingRecord(invoiceId, file, storedFilePath, fileHash);

        await _uploadedInvoiceStore.SaveAsync(record, cancellationToken);

        try
        {
            var uploadedFile = CreateUploadedFolderInvoiceFile(file, storedFilePath);
            var parseResult = await _invoiceParser.ParseAsync(uploadedFile, cancellationToken);

            List<string> missingFields = _invoiceParseResultValidator.Validate(parseResult);

            if (missingFields.Count > 0)
            {
                var invalidRecord = CreateInvalidRecord(record, parseResult, missingFields);

                await _uploadedInvoiceStore.SaveAsync(invalidRecord, cancellationToken);

                return new UploadInvoiceAcceptedResponse
                {
                    InvoiceId = invoiceId,
                    Status = InvoiceStatuses.Invalid,
                    Message = invalidRecord.Message ?? InvoiceMessages.ParsingFailed,
                    MissingFields = missingFields   
                };
            }

            var supplierMatchResult = await _supplierMatcher.MatchAsync(parseResult, cancellationToken);

            var isReadyToPost =
                supplierMatchResult.IsMatched &&
                !supplierMatchResult.RequiresReview &&
                !string.IsNullOrWhiteSpace(supplierMatchResult.ExactSupplierId);

            var parsedRecord = CreateParsedRecord(record, parseResult, supplierMatchResult);

            await _uploadedInvoiceStore.SaveAsync(parsedRecord, cancellationToken);

            if (isReadyToPost)
            {
                await _exactPostOutboxWriter.EnqueueAsync(parsedRecord.InvoiceId, cancellationToken);
            }

            return new UploadInvoiceAcceptedResponse
            {
                InvoiceId = invoiceId,
                Status = parsedRecord.Status,
                Message = parsedRecord.Message ?? InvoiceMessages.ParsingFailed,
                MissingFields = new List<string>()
            };
        }
        catch (Exception)
        {
            await _uploadedInvoiceStore.UpdateStatusAsync(
                invoiceId,
                InvoiceStatuses.Failed,
                InvoiceMessages.ParsingFailed,
                cancellationToken);

            return new UploadInvoiceAcceptedResponse
            {
                InvoiceId = invoiceId,
                Status = InvoiceStatuses.Failed,
                Message =InvoiceMessages.ParsingFailed, 
                MissingFields = new List<string>()
            };
        }
    }

    private static async Task<string> ComputeFileHashAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);

        return Convert.ToHexString(hashBytes);
    }

    private static FolderInvoiceFile CreateUploadedFolderInvoiceFile(IFormFile file, string storedFilePath)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(storedFilePath);

        return new FolderInvoiceFile
        {
            FileName = file.FileName,
            FullPath = storedFilePath,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? GetContentTypeFromFileName(file.FileName)
                : file.ContentType
        };
    }

    private static string GetContentTypeFromFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".tif" => "image/tiff",
            ".tiff" => "image/tiff",
            _ => "application/octet-stream"
        };
    }
    private static bool IsReadyToPost(SupplierMatchResult supplierMatchResult)
    {
        ArgumentNullException.ThrowIfNull(supplierMatchResult);

        return supplierMatchResult.IsMatched &&
               !supplierMatchResult.RequiresReview &&
               !string.IsNullOrWhiteSpace(supplierMatchResult.ExactSupplierId);
    }

    private static UploadedInvoiceRecord CreateProcessingRecord(
    string invoiceId,
    IFormFile file,
    string storedFilePath,
    string fileHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceId);
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(storedFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileHash);

        return new UploadedInvoiceRecord
        {
            InvoiceId = invoiceId,
            OriginalFileName = file.FileName,
            StoredFilePath = storedFilePath,
            Status = InvoiceStatuses.Processing,
            Message = InvoiceMessages.UploadReceived,
            CreatedAtUtc = DateTime.UtcNow,
            FileHash = fileHash
        };
    }

    private static UploadedInvoiceRecord CreateInvalidRecord(
    UploadedInvoiceRecord processingRecord,
    InvoiceParseResult parseResult,
    List<string> missingFields)
    {
        ArgumentNullException.ThrowIfNull(processingRecord);
        ArgumentNullException.ThrowIfNull(parseResult);
        ArgumentNullException.ThrowIfNull(missingFields);

        return new UploadedInvoiceRecord
        {
            InvoiceId = processingRecord.InvoiceId,
            OriginalFileName = processingRecord.OriginalFileName,
            StoredFilePath = processingRecord.StoredFilePath,
            Status = InvoiceStatuses.Invalid,
            Message = InvoiceMessages.MissingRequiredFields(missingFields),
            CreatedAtUtc = processingRecord.CreatedAtUtc,
            FileHash = processingRecord.FileHash,
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
            SupplierMatchMessage = null,
            ExactPostingStatus = null,
            ExactDocumentId = null,
            PostedToExactAtUtc = null,
            ExactPostingError = null
        };
    }

    private static UploadedInvoiceRecord CreateParsedRecord(
    UploadedInvoiceRecord processingRecord,
    InvoiceParseResult parseResult,
    SupplierMatchResult supplierMatchResult)
    {
        ArgumentNullException.ThrowIfNull(processingRecord);
        ArgumentNullException.ThrowIfNull(parseResult);
        ArgumentNullException.ThrowIfNull(supplierMatchResult);

        bool isReadyToPost = IsReadyToPost(supplierMatchResult);

        return new UploadedInvoiceRecord
        {
            InvoiceId = processingRecord.InvoiceId,
            OriginalFileName = processingRecord.OriginalFileName,
            StoredFilePath = processingRecord.StoredFilePath,
            Status = isReadyToPost
                ? InvoiceStatuses.ReadyToPost
                : InvoiceStatuses.Parsed,
            Message = isReadyToPost
                ? InvoiceMessages.ReadyToPost
                : InvoiceMessages.ParsedButRequiresSupplierReview,
            CreatedAtUtc = processingRecord.CreatedAtUtc,
            FileHash = processingRecord.FileHash,
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
            SupplierMatchMessage = supplierMatchResult.Message,
            ExactPostingStatus = isReadyToPost
                ? ExactPostingStatuses.Queued
                : null,
            ExactDocumentId = null,
            PostedToExactAtUtc = null,
            ExactPostingError = null
        };
    }
}