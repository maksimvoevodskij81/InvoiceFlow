namespace InvoiceFlow.Api.Features.Exact;

public interface IExactPostOutboxWriter
{
    Task EnqueueAsync(string invoiceId, CancellationToken cancellationToken = default);
    Task RequeueAsync(string invoiceId, CancellationToken cancellationToken = default);
}