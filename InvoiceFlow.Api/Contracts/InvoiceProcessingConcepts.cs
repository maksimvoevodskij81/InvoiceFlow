namespace InvoiceFlow.Api.Contracts;

public static class InvoiceProcessingConcepts
{
    public const string SupplierMatching = "SupplierMatching";
    public const string RequestIdempotency = "RequestIdempotency";
    public const string BusinessDuplicateDetection = "BusinessDuplicateDetection";
    public const string IdempotencyKeyHeaderName = "Idempotency-Key";
}