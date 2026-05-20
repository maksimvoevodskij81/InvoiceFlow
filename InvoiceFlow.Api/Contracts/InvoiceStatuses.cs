namespace InvoiceFlow.Api.Contracts;

public static class InvoiceStatuses
{
    public const string Uploaded = "Uploaded";
    public const string Parsed = "Parsed";
    public const string Processing = "Processing";
    public const string Failed = "Failed";
    public const string Duplicate = "Duplicate";
    public const string Invalid = "Invalid";
    public const string ReadyToPost = "ReadyToPost";
    public const string NeedsReview = "NeedsReview";
    public const string ExtractionFailed = "ExtractionFailed";
}