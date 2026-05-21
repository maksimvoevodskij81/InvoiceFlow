namespace InvoiceFlow.Api.Features.Invoices.RetryExtraction;

public interface IInvoiceRetryService
{
    Task<RetryExtractionResponse> RetryExtractionAsync(string invoiceId, CancellationToken cancellationToken = default);
}
