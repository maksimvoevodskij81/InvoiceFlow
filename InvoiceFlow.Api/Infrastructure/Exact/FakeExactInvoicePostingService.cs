using InvoiceFlow.Api.Features.Exact;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;

namespace InvoiceFlow.Api.Infrastructure.Exact;

public sealed class FakeExactInvoicePostingService : IExactInvoicePostingService
{
    public Task<ExactPostResult> PostAsync(UploadedInvoiceRecord invoice, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var result = new ExactPostResult
        {
            Success = true,
            ExternalDocumentId = $"EXACT-{invoice.InvoiceId}"
        };

        return Task.FromResult(result);
    }
}