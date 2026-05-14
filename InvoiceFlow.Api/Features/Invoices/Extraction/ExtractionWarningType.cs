namespace InvoiceFlow.Api.Features.Invoices.Extraction;

public enum ExtractionWarningType
{
    LowConfidence,
    AmbiguousFormat,
    MissingOptional,
    ParseFailed
}
