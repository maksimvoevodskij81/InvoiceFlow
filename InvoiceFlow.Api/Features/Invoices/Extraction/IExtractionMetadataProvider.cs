namespace InvoiceFlow.Api.Features.Invoices.Extraction;

public interface IExtractionMetadataProvider
{
    LlmExtractionResult? LastExtractionResult { get; }
}
