namespace InvoiceFlow.Api.Infrastructure.Persistence;

public sealed class UploadedInvoiceEntity
{
    public required string InvoiceId { get; set; }

    public required string OriginalFileName { get; set; }

    public required string StoredFilePath { get; set; }

    public required string Status { get; set; }

    public string? Message { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public string? ReviewDecision { get; set; }

    public string? ReviewComment { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public required string FileHash { get; set; }

    public string? SupplierName { get; set; }

    public string? InvoiceNumber { get; set; }

    public DateOnly? InvoiceDate { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? Currency { get; set; }

    public bool IsSupplierMatched { get; set; }

    public bool RequiresSupplierReview { get; set; }

    public string? SupplierMatchedBy { get; set; }

    public string? InternalSupplierId { get; set; }

    public string? ExactSupplierId { get; set; }

    public string? SupplierMatchMessage { get; set; }
    public string? ExactPostingStatus { get; set; }

    public string? ExactDocumentId { get; set; }

    public DateTime? PostedToExactAtUtc { get; set; }

    public string? ExactPostingError { get; set; }
    public bool CanCreateSupplier { get; set; }
    public string? SupplierAddressLine { get; set; }
    public string? SupplierPostcode { get; set; }
    public string? SupplierCity { get; set; }
    public string? SupplierCountry { get; set; }
    public string? SupplierBankAccount { get; set; }
    public string? SupplierBicCode { get; set; }
    public bool HasNewBankDetails { get; set; }

    public string? ExtractionModel { get; set; }

    public DateTime? ExtractionCompletedAtUtc { get; set; }

    public string? RawExtractionJson { get; set; }

    public List<string> ExtractionWarnings { get; set; } = new();

    public string? ExtractionError { get; set; }

    public List<string> MatchReasons { get; set; } = new();
}