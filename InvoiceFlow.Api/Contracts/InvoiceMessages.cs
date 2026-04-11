namespace InvoiceFlow.Api.Features.Invoices;

public static class InvoiceMessages
{
    public const string UploadReceived = "Invoice upload received.";
    public const string DuplicateUploadDetected = "Duplicate invoice upload detected.";
    public const string ParsingFailed = "Invoice parsing failed.";

    public const string ParsedSuccessfully = "Invoice parsed successfully.";
    public const string ParsedButRequiresSupplierReview = "Invoice parsed successfully, but requires supplier review.";
    public const string ReadyToPost = "Invoice parsed, validated, and ready to post.";

    public static string MissingRequiredFields(IEnumerable<string> missingFields)
    {
        return $"Missing required fields: {string.Join(", ", missingFields)}";
    }
}