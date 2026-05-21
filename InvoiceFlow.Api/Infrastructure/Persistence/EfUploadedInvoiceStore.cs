using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Invoices;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Api.Infrastructure.Persistence;

public sealed class EfUploadedInvoiceStore : IUploadedInvoiceStore
{
    private readonly InvoiceFlowDbContext _dbContext;

    public EfUploadedInvoiceStore(InvoiceFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAsync(UploadedInvoiceRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var existingEntity = await _dbContext.UploadedInvoices
            .SingleOrDefaultAsync(x => x.InvoiceId == record.InvoiceId, cancellationToken);

        if (existingEntity is null)
        {
            var newEntity = MapToEntity(record);

            _dbContext.UploadedInvoices.Add(newEntity);
        }
        else
        {
            UpdateEntity(existingEntity, record);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<UploadedInvoiceRecord?> GetByIdAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceId);

        var entity = await _dbContext.UploadedInvoices
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.InvoiceId == invoiceId, cancellationToken);

        return entity is null ? null : MapToRecord(entity);
    }

    public async Task<UploadedInvoiceRecord?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileHash);

        var entity = await _dbContext.UploadedInvoices
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.FileHash == fileHash, cancellationToken);

        return entity is null ? null : MapToRecord(entity);
    }

    public async Task<IReadOnlyList<UploadedInvoiceRecord>> QueryAsync(
        InvoiceListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<UploadedInvoiceEntity> source = _dbContext.UploadedInvoices.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(x => x.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.ReviewDecision))
        {
            source = source.Where(x => x.ReviewDecision == query.ReviewDecision);
        }

        if (query.CanCreateSupplier.HasValue)
        {
            source = source.Where(x => x.CanCreateSupplier == query.CanCreateSupplier.Value);
        }

        source = source.OrderByDescending(x => x.CreatedAtUtc);

        var entities = await source.ToListAsync(cancellationToken);

        return entities.Select(MapToRecord).ToList();
    }

    public async Task UpdateStatusAsync(
        string invoiceId,
        string status,
        string? message,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        var entity = await _dbContext.UploadedInvoices
            .SingleOrDefaultAsync(x => x.InvoiceId == invoiceId, cancellationToken);

        if (entity is null)
        {
            return;
        }

        entity.Status = status;
        entity.Message = message;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSupplierCreationResultAsync(
        string invoiceId,
        string exactSupplierId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(exactSupplierId);

        var entity = await _dbContext.UploadedInvoices
            .SingleOrDefaultAsync(x => x.InvoiceId == invoiceId, cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException($"Uploaded invoice with id '{invoiceId}' was not found.");
        }

        entity.IsSupplierMatched = true;
        entity.RequiresSupplierReview = false;
        entity.CanCreateSupplier = false;

        entity.ExactSupplierId = exactSupplierId;
        entity.SupplierMatchedBy = SupplierMatchSources.CreatedInExact;
        entity.SupplierMatchMessage = InvoiceMessages.SupplierCreatedInExactSuccessfully;

        entity.Status = InvoiceStatuses.ReadyToPost;
        entity.Message = InvoiceMessages.ReadyToPost;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static UploadedInvoiceEntity MapToEntity(UploadedInvoiceRecord record)
    {
        return new UploadedInvoiceEntity
        {
            InvoiceId = record.InvoiceId,
            OriginalFileName = record.OriginalFileName,
            StoredFilePath = record.StoredFilePath,
            Status = record.Status,
            Message = record.Message,
            CreatedAtUtc = record.CreatedAtUtc,
            FileHash = record.FileHash,
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
            ReviewedAtUtc = record.ReviewedAtUtc,
            ReviewDecision = record.ReviewDecision,
            ReviewComment = record.ReviewComment,
            CanCreateSupplier = record.CanCreateSupplier,
            SupplierAddressLine = record.SupplierAddressLine,
            SupplierPostcode = record.SupplierPostcode,
            SupplierCity = record.SupplierCity,
            SupplierCountry = record.SupplierCountry,
            SupplierBankAccount = record.SupplierBankAccount,
            SupplierBicCode = record.SupplierBicCode,
            HasNewBankDetails = record.HasNewBankDetails,
            ExtractionModel = record.ExtractionModel,
            ExtractionCompletedAtUtc = record.ExtractionCompletedAtUtc,
            RawExtractionJson = record.RawExtractionJson,
            ExtractionWarnings = record.ExtractionWarnings,
            ExtractionError = record.ExtractionError,
            MatchReasons = record.MatchReasons,
            AcceptedSupplierName  = record.AcceptedSupplierName,
            AcceptedInvoiceNumber = record.AcceptedInvoiceNumber,
            AcceptedInvoiceDate   = record.AcceptedInvoiceDate,
            AcceptedTotalAmount   = record.AcceptedTotalAmount,
            AcceptedCurrency      = record.AcceptedCurrency
        };
    }

    private static UploadedInvoiceRecord MapToRecord(UploadedInvoiceEntity entity)
    {
        return new UploadedInvoiceRecord
        {
            InvoiceId = entity.InvoiceId,
            OriginalFileName = entity.OriginalFileName,
            StoredFilePath = entity.StoredFilePath,
            Status = entity.Status,
            Message = entity.Message,
            CreatedAtUtc = entity.CreatedAtUtc,
            FileHash = entity.FileHash,
            SupplierName = entity.SupplierName,
            InvoiceNumber = entity.InvoiceNumber,
            InvoiceDate = entity.InvoiceDate,
            TotalAmount = entity.TotalAmount,
            Currency = entity.Currency,
            IsSupplierMatched = entity.IsSupplierMatched,
            RequiresSupplierReview = entity.RequiresSupplierReview,
            SupplierMatchedBy = entity.SupplierMatchedBy,
            InternalSupplierId = entity.InternalSupplierId,
            ExactSupplierId = entity.ExactSupplierId,
            SupplierMatchMessage = entity.SupplierMatchMessage,
            ExactPostingStatus = entity.ExactPostingStatus,
            ExactDocumentId = entity.ExactDocumentId,
            PostedToExactAtUtc = entity.PostedToExactAtUtc,
            ExactPostingError = entity.ExactPostingError,
            ReviewedAtUtc = entity.ReviewedAtUtc,
            ReviewDecision = entity.ReviewDecision,
            ReviewComment = entity.ReviewComment,
            CanCreateSupplier = entity.CanCreateSupplier,
            SupplierAddressLine = entity.SupplierAddressLine,
            SupplierPostcode = entity.SupplierPostcode,
            SupplierCity = entity.SupplierCity,
            SupplierCountry = entity.SupplierCountry,
            SupplierBankAccount = entity.SupplierBankAccount,
            SupplierBicCode = entity.SupplierBicCode,
            HasNewBankDetails = entity.HasNewBankDetails,
            ExtractionModel = entity.ExtractionModel,
            ExtractionCompletedAtUtc = entity.ExtractionCompletedAtUtc,
            RawExtractionJson = entity.RawExtractionJson,
            ExtractionWarnings = entity.ExtractionWarnings,
            ExtractionError = entity.ExtractionError,
            MatchReasons = entity.MatchReasons,
            AcceptedSupplierName  = entity.AcceptedSupplierName,
            AcceptedInvoiceNumber = entity.AcceptedInvoiceNumber,
            AcceptedInvoiceDate   = entity.AcceptedInvoiceDate,
            AcceptedTotalAmount   = entity.AcceptedTotalAmount,
            AcceptedCurrency      = entity.AcceptedCurrency
        };
    }

    private static void UpdateEntity(UploadedInvoiceEntity entity, UploadedInvoiceRecord record)
    {
        entity.OriginalFileName = record.OriginalFileName;
        entity.StoredFilePath = record.StoredFilePath;
        entity.Status = record.Status;
        entity.Message = record.Message;
        entity.CreatedAtUtc = record.CreatedAtUtc;
        entity.FileHash = record.FileHash;
        entity.SupplierName = record.SupplierName;
        entity.InvoiceNumber = record.InvoiceNumber;
        entity.InvoiceDate = record.InvoiceDate;
        entity.TotalAmount = record.TotalAmount;
        entity.Currency = record.Currency;
        entity.IsSupplierMatched = record.IsSupplierMatched;
        entity.RequiresSupplierReview = record.RequiresSupplierReview;
        entity.SupplierMatchedBy = record.SupplierMatchedBy;
        entity.InternalSupplierId = record.InternalSupplierId;
        entity.ExactSupplierId = record.ExactSupplierId;
        entity.SupplierMatchMessage = record.SupplierMatchMessage;
        entity.ExactPostingStatus = record.ExactPostingStatus;
        entity.ExactDocumentId = record.ExactDocumentId;
        entity.PostedToExactAtUtc = record.PostedToExactAtUtc;
        entity.ExactPostingError = record.ExactPostingError;
        entity.ReviewedAtUtc = record.ReviewedAtUtc;
        entity.ReviewDecision = record.ReviewDecision;
        entity.ReviewComment = record.ReviewComment;
        entity.CanCreateSupplier = record.CanCreateSupplier;
        entity.SupplierAddressLine = record.SupplierAddressLine;
        entity.SupplierPostcode = record.SupplierPostcode;
        entity.SupplierCity = record.SupplierCity;
        entity.SupplierCountry = record.SupplierCountry;
        entity.SupplierBankAccount = record.SupplierBankAccount;
        entity.SupplierBicCode = record.SupplierBicCode;
        entity.HasNewBankDetails = record.HasNewBankDetails;
        entity.ExtractionModel = record.ExtractionModel;
        entity.ExtractionCompletedAtUtc = record.ExtractionCompletedAtUtc;
        entity.RawExtractionJson = record.RawExtractionJson;
        entity.ExtractionWarnings = record.ExtractionWarnings;
        entity.ExtractionError = record.ExtractionError;
        entity.MatchReasons = record.MatchReasons;
        entity.AcceptedSupplierName  = record.AcceptedSupplierName;
        entity.AcceptedInvoiceNumber = record.AcceptedInvoiceNumber;
        entity.AcceptedInvoiceDate   = record.AcceptedInvoiceDate;
        entity.AcceptedTotalAmount   = record.AcceptedTotalAmount;
        entity.AcceptedCurrency      = record.AcceptedCurrency;
    }
}