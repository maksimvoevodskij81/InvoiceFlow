using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Exact;
using InvoiceFlow.Api.Features.Invoices.Extraction;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using InvoiceFlow.Api.Features.Suppliers.CreateSupplier;
using InvoiceFlow.Api.Features.Suppliers.Matching;

namespace InvoiceFlow.Api.Features.Invoices.RetryExtraction;

public sealed class InvoiceRetryService : IInvoiceRetryService
{
    private readonly IUploadedInvoiceStore _uploadedInvoiceStore;
    private readonly IInvoiceParser _invoiceParser;
    private readonly ISupplierMatcher _supplierMatcher;
    private readonly IBankDetailsRiskEvaluator _bankDetailsRiskEvaluator;
    private readonly InvoiceParseResultValidator _invoiceParseResultValidator;
    private readonly SupplierCreateValidator _supplierCreateValidator;
    private readonly IExactPostOutboxWriter _exactPostOutboxWriter;
    private readonly ISupplierCreateOutboxWriter _supplierCreateOutboxWriter;

    public InvoiceRetryService(
        IUploadedInvoiceStore uploadedInvoiceStore,
        IInvoiceParser invoiceParser,
        ISupplierMatcher supplierMatcher,
        IBankDetailsRiskEvaluator bankDetailsRiskEvaluator,
        InvoiceParseResultValidator invoiceParseResultValidator,
        SupplierCreateValidator supplierCreateValidator,
        IExactPostOutboxWriter exactPostOutboxWriter,
        ISupplierCreateOutboxWriter supplierCreateOutboxWriter)
    {
        _uploadedInvoiceStore = uploadedInvoiceStore;
        _invoiceParser = invoiceParser;
        _supplierMatcher = supplierMatcher;
        _bankDetailsRiskEvaluator = bankDetailsRiskEvaluator;
        _invoiceParseResultValidator = invoiceParseResultValidator;
        _supplierCreateValidator = supplierCreateValidator;
        _exactPostOutboxWriter = exactPostOutboxWriter;
        _supplierCreateOutboxWriter = supplierCreateOutboxWriter;
    }

    public async Task<RetryExtractionResponse> RetryExtractionAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceId);

        var invoice = await _uploadedInvoiceStore.GetByIdAsync(invoiceId, cancellationToken);

        if (invoice is null)
        {
            throw new KeyNotFoundException($"Invoice with id '{invoiceId}' was not found.");
        }

        if (invoice.Status != InvoiceStatuses.ExtractionFailed)
        {
            throw new InvalidOperationException(
                $"Invoice '{invoiceId}' is not in '{InvoiceStatuses.ExtractionFailed}' status.");
        }

        var extractionModel = _invoiceParser.GetType().Name;
        LlmExtractionResult? llmResult = null;

        var file = new FolderInvoiceFile
        {
            FileName    = invoice.OriginalFileName,
            FullPath    = invoice.StoredFilePath,
            ContentType = GetContentTypeFromFileName(invoice.OriginalFileName)
        };

        try
        {
            var parseResult = await _invoiceParser.ParseAsync(file, cancellationToken);

            llmResult = (_invoiceParser as IExtractionMetadataProvider)?.LastExtractionResult;

            if (llmResult is { IsSuccessful: false })
            {
                ApplyExtractionFailure(invoice, extractionModel, llmResult);
                await _uploadedInvoiceStore.SaveAsync(invoice, cancellationToken);
                return BuildResponse(invoice);
            }

            var missingFields = _invoiceParseResultValidator.Validate(parseResult);

            if (missingFields.Count > 0)
            {
                ApplyInvalidResult(invoice, parseResult, missingFields, extractionModel, llmResult);
                await _uploadedInvoiceStore.SaveAsync(invoice, cancellationToken);
                return BuildResponse(invoice);
            }

            var supplierMatchResult = await _supplierMatcher.MatchAsync(parseResult, cancellationToken);

            if (supplierMatchResult.IsMatched && !string.IsNullOrWhiteSpace(supplierMatchResult.ExactSupplierId))
            {
                var bankRisk = await _bankDetailsRiskEvaluator.EvaluateAsync(
                    parseResult,
                    supplierMatchResult.ExactSupplierId,
                    cancellationToken);

                supplierMatchResult.Reasons.AddRange(bankRisk.Reasons);

                if (bankRisk.HasConflict)
                {
                    supplierMatchResult.RequiresReview = true;
                    supplierMatchResult.HasNewBankDetails = false;
                    supplierMatchResult.Message = "Supplier matched, but bank account conflicts with another supplier.";
                }
                else if (bankRisk.IsNewBankDetails)
                {
                    supplierMatchResult.RequiresReview = true;
                    supplierMatchResult.HasNewBankDetails = true;
                    supplierMatchResult.Message = "Supplier matched, but bank details are new and require review.";
                }
            }

            var missingSupplierFields = _supplierCreateValidator.Validate(parseResult);
            var canCreateSupplier = !supplierMatchResult.IsMatched && missingSupplierFields.Count == 0;
            var isReadyToPost = supplierMatchResult.IsMatched
                && !supplierMatchResult.RequiresReview
                && !string.IsNullOrWhiteSpace(supplierMatchResult.ExactSupplierId);

            ApplyParsedResult(invoice, parseResult, supplierMatchResult, canCreateSupplier, extractionModel, llmResult);
            await _uploadedInvoiceStore.SaveAsync(invoice, cancellationToken);

            if (canCreateSupplier)
            {
                await _supplierCreateOutboxWriter.EnqueueAsync(invoiceId, cancellationToken);
            }

            if (isReadyToPost)
            {
                await _exactPostOutboxWriter.EnqueueAsync(invoiceId, cancellationToken);
            }

            return BuildResponse(invoice);
        }
        catch (Exception) when (invoice.Status != InvoiceStatuses.ExtractionFailed)
        {
            invoice.Status = InvoiceStatuses.Failed;
            invoice.Message = InvoiceMessages.ParsingFailed;
            invoice.ExtractionCompletedAtUtc = llmResult?.Metadata.ExtractedAtUtc ?? DateTime.UtcNow;
            invoice.ExtractionModel = llmResult?.Metadata.Model ?? extractionModel;
            invoice.RawExtractionJson = llmResult?.Raw.RawJson;
            invoice.ExtractionWarnings = llmResult?.Metadata.Warnings
                .Select(w => $"{w.Type} [{w.Field}]: {w.Message}").ToList() ?? [];
            invoice.ExtractionError = llmResult?.Error is { } e
                ? $"{e.Code}: {e.Message}"
                : InvoiceMessages.ParsingFailed;

            await _uploadedInvoiceStore.SaveAsync(invoice, cancellationToken);
            return BuildResponse(invoice);
        }
    }

    private static void ApplyExtractionFailure(UploadedInvoiceRecord invoice, string extractionModel, LlmExtractionResult llmResult)
    {
        invoice.Status = InvoiceStatuses.ExtractionFailed;
        invoice.Message = InvoiceMessages.ExtractionFailed;
        invoice.ExtractionCompletedAtUtc = llmResult.Metadata.ExtractedAtUtc;
        invoice.ExtractionModel = llmResult.Metadata.Model ?? extractionModel;
        invoice.RawExtractionJson = llmResult.Raw.RawJson;
        invoice.ExtractionWarnings = llmResult.Metadata.Warnings
            .Select(w => $"{w.Type} [{w.Field}]: {w.Message}").ToList();
        invoice.ExtractionError = llmResult.Error is { } e ? $"{e.Code}: {e.Message}" : null;
    }

    private static void ApplyInvalidResult(
        UploadedInvoiceRecord invoice,
        InvoiceParseResult parseResult,
        List<string> missingFields,
        string extractionModel,
        LlmExtractionResult? llmResult)
    {
        invoice.Status = InvoiceStatuses.Invalid;
        invoice.Message = InvoiceMessages.MissingRequiredFields(missingFields);
        invoice.SupplierName = parseResult.SupplierName;
        invoice.InvoiceNumber = parseResult.InvoiceNumber;
        invoice.InvoiceDate = parseResult.InvoiceDate;
        invoice.TotalAmount = parseResult.TotalAmount;
        invoice.Currency = parseResult.Currency;
        invoice.IsSupplierMatched = false;
        invoice.RequiresSupplierReview = false;
        invoice.ExtractionCompletedAtUtc = llmResult?.Metadata.ExtractedAtUtc ?? DateTime.UtcNow;
        invoice.ExtractionModel = llmResult?.Metadata.Model ?? extractionModel;
        invoice.RawExtractionJson = llmResult?.Raw.RawJson;
        invoice.ExtractionWarnings = llmResult?.Metadata.Warnings
            .Select(w => $"{w.Type} [{w.Field}]: {w.Message}").ToList() ?? [];
        invoice.ExtractionError = null;
    }

    private static void ApplyParsedResult(
        UploadedInvoiceRecord invoice,
        InvoiceParseResult parseResult,
        SupplierMatchResult supplierMatchResult,
        bool canCreateSupplier,
        string extractionModel,
        LlmExtractionResult? llmResult)
    {
        var isReadyToPost = supplierMatchResult.IsMatched
            && !supplierMatchResult.RequiresReview
            && !string.IsNullOrWhiteSpace(supplierMatchResult.ExactSupplierId);

        var needsReview = supplierMatchResult.RequiresReview
            || (!supplierMatchResult.IsMatched && !canCreateSupplier);

        if (isReadyToPost)
        {
            invoice.Status = InvoiceStatuses.ReadyToPost;
            invoice.Message = InvoiceMessages.ReadyToPost;
        }
        else if (needsReview)
        {
            invoice.Status = InvoiceStatuses.NeedsReview;
            invoice.Message = InvoiceMessages.NeedsReview;
        }
        else
        {
            invoice.Status = InvoiceStatuses.Parsed;
            invoice.Message = InvoiceMessages.ParsedButRequiresSupplierReview;
        }

        invoice.SupplierName = parseResult.SupplierName;
        invoice.InvoiceNumber = parseResult.InvoiceNumber;
        invoice.InvoiceDate = parseResult.InvoiceDate;
        invoice.TotalAmount = parseResult.TotalAmount;
        invoice.Currency = parseResult.Currency;
        invoice.SupplierAddressLine = parseResult.SupplierAddressLine;
        invoice.SupplierPostcode = parseResult.SupplierPostcode;
        invoice.SupplierCity = parseResult.SupplierCity;
        invoice.SupplierCountry = parseResult.SupplierCountry;
        invoice.SupplierBankAccount = parseResult.SupplierBankAccount;
        invoice.SupplierBicCode = parseResult.SupplierBicCode;
        invoice.IsSupplierMatched = supplierMatchResult.IsMatched;
        invoice.RequiresSupplierReview = supplierMatchResult.RequiresReview;
        invoice.SupplierMatchedBy = supplierMatchResult.MatchedBy;
        invoice.InternalSupplierId = supplierMatchResult.InternalSupplierId;
        invoice.ExactSupplierId = supplierMatchResult.ExactSupplierId;
        invoice.SupplierMatchMessage = supplierMatchResult.Message;
        invoice.CanCreateSupplier = canCreateSupplier;
        invoice.HasNewBankDetails = supplierMatchResult.HasNewBankDetails;
        invoice.MatchReasons = supplierMatchResult.Reasons;
        invoice.ExactPostingStatus = isReadyToPost ? ExactPostingStatuses.Queued : null;
        invoice.ExtractionCompletedAtUtc = llmResult?.Metadata.ExtractedAtUtc ?? DateTime.UtcNow;
        invoice.ExtractionModel = llmResult?.Metadata.Model ?? extractionModel;
        invoice.RawExtractionJson = llmResult?.Raw.RawJson;
        invoice.ExtractionWarnings = llmResult?.Metadata.Warnings
            .Select(w => $"{w.Type} [{w.Field}]: {w.Message}").ToList() ?? [];
        invoice.ExtractionError = null;
    }

    private static RetryExtractionResponse BuildResponse(UploadedInvoiceRecord invoice)
    {
        return new RetryExtractionResponse
        {
            InvoiceId = invoice.InvoiceId,
            Status    = invoice.Status,
            Message   = invoice.Message ?? string.Empty
        };
    }

    private static string GetContentTypeFromFileName(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf"  => "application/pdf",
            ".jpg"  => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png"  => "image/png",
            ".tif"  => "image/tiff",
            ".tiff" => "image/tiff",
            _       => "application/octet-stream"
        };
    }
}
