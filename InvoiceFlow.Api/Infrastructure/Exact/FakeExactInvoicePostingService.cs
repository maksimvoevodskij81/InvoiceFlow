using InvoiceFlow.Api.Features.Exact;

namespace InvoiceFlow.Api.Infrastructure.Exact;

public sealed class FakeExactInvoicePostingService : IExactInvoicePostingService
{
    public Task<ExactInvoicePostingResponse> PostAsync(ExactInvoicePostingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = new ExactInvoicePostingResponse
        {
            IsSuccess = true,
            ExactDocumentId = $"fake-doc-{request.InvoiceNumber}",
            ErrorMessage = null
        };

        return Task.FromResult(result);
    }
}