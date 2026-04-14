namespace InvoiceFlow.Api.Features.Exact
{
    public sealed class ExactOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiBaseUrl { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
