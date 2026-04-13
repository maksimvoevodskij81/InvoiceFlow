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
            ExactPostingError = record.ExactPostingError
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
            ExactPostingError = entity.ExactPostingError
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
    }
}