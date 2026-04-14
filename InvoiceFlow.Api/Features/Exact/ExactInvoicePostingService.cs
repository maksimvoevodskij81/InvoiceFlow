using Microsoft.Extensions.Options;

namespace InvoiceFlow.Api.Features.Exact;

public sealed class ExactInvoicePostingService : IExactInvoicePostingService
{
    private readonly HttpClient _httpClient;
    private readonly ExactOptions _options;

    public ExactInvoicePostingService(
        HttpClient httpClient,
        IOptions<ExactOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<ExactInvoicePostingResponse> PostAsync(
        ExactInvoicePostingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // TODO: replace endpoint with the real Exact endpoint you choose
        var endpoint = $"{_options.ApiBaseUrl}/v1/{_options.Division}/purchaseentry/PurchaseEntries";

        var response = await _httpClient.PostAsJsonAsync(
            endpoint,
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ExactInvoicePostingResponse
            {
                IsSuccess = false,
                ErrorMessage = error
            };
        }

        // TODO: replace parsing with the real Exact response shape
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        return new ExactInvoicePostingResponse
        {
            IsSuccess = true,
            ExactDocumentId = content
        };
    }
}
