using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using Microsoft.AspNetCore.Http;

namespace InvoiceFlow.Api.Features.Invoices.UploadInvoice;

public sealed class InvoiceUploadService : IInvoiceUploadService
{
    private readonly IInvoiceParser _invoiceParser;
    private readonly ISupplierMatcher _supplierMatcher;
    private readonly IUploadedInvoiceFileStore _uploadedInvoiceFileStore;
    private readonly IUploadedInvoiceStore _uploadedInvoiceStore;

    public InvoiceUploadService(
        IInvoiceParser invoiceParser,
        ISupplierMatcher supplierMatcher,
        IUploadedInvoiceFileStore uploadedInvoiceFileStore,
        IUploadedInvoiceStore uploadedInvoiceStore)
    {
        _invoiceParser = invoiceParser;
        _supplierMatcher = supplierMatcher;
        _uploadedInvoiceFileStore = uploadedInvoiceFileStore;
        _uploadedInvoiceStore = uploadedInvoiceStore;
    }

    public async Task<UploadInvoiceAcceptedResponse> UploadAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        var invoiceId = Guid.NewGuid().ToString();
        var storedFilePath = await _uploadedInvoiceFileStore.SaveAsync(file, cancellationToken);

        var record = new UploadedInvoiceRecord
        {
            InvoiceId = invoiceId,
            OriginalFileName = file.FileName,
            StoredFilePath = storedFilePath,
            Status = InvoiceStatuses.Processing,
            Message = "Invoice upload received.",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _uploadedInvoiceStore.SaveAsync(record, cancellationToken);

        try
        {
            var uploadedFile = CreateUploadedFolderInvoiceFile(file, storedFilePath);
            var parseResult = await _invoiceParser.ParseAsync(uploadedFile, cancellationToken);
            var supplierMatchResult = await _supplierMatcher.MatchAsync(parseResult);

            var parsedRecord = new UploadedInvoiceRecord
            {
                InvoiceId = record.InvoiceId,
                OriginalFileName = record.OriginalFileName,
                StoredFilePath = record.StoredFilePath,
                Status = InvoiceStatuses.Parsed,
                Message = "Invoice parsed successfully.",
                CreatedAtUtc = record.CreatedAtUtc,
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

            await _uploadedInvoiceStore.SaveAsync(parsedRecord, cancellationToken);

            return new UploadInvoiceAcceptedResponse
            {
                InvoiceId = invoiceId,
                Status = InvoiceStatuses.Parsed,
                Message = "Invoice parsed successfully."
            };
        }
        catch (Exception)
        {
            await _uploadedInvoiceStore.UpdateStatusAsync(
                invoiceId,
                InvoiceStatuses.Failed,
                "Invoice parsing failed.",
                cancellationToken);

            return new UploadInvoiceAcceptedResponse
            {
                InvoiceId = invoiceId,
                Status = InvoiceStatuses.Failed,
                Message = "Invoice parsing failed."
            };
        }
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
}