namespace InvoiceFlow.Api.Infrastructure.Persistence;

public sealed class ExactPostOutboxEntity
{
    public Guid Id { get; set; }

    public required string InvoiceId { get; set; }

    public required string Status { get; set; }

    public int AttemptCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastAttemptAtUtc { get; set; }

    public DateTime? NextAttemptAtUtc { get; set; }

    public string? ExternalDocumentId { get; set; }

    public string? LastError { get; set; }
}