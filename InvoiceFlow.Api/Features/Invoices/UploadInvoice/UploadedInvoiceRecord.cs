namespace InvoiceFlow.Api.Features.Invoices.UploadInvoice
{
    public sealed class UploadedInvoiceRecord
    {
        public required string InvoiceId { get; init; }

        public required string OriginalFileName { get; init; }

        public required string StoredFilePath { get; init; }

        public required string Status { get; set; }

        public string? Message { get; set; }

        public DateTime CreatedAtUtc { get; init; }
    }
}
