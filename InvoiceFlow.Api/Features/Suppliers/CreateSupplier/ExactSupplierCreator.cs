namespace InvoiceFlow.Api.Features.Suppliers.CreateSupplier
{
    public sealed class ExactSupplierCreator : ISupplierCreator
    {
        private readonly HttpClient _httpClient;

        public ExactSupplierCreator(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> CreateAsync(
            SupplierCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            var payload = new
            {
                Name = request.Name,
                AddressLine1 = request.AddressLine,
                Postcode = request.Postcode,
                City = request.City,
                Country = request.Country,
                BankAccount = request.BankAccount,
                BicCode = request.BicCode
            };

            var response = await _httpClient.PostAsJsonAsync(
                "api/suppliers",
                payload,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ExactSupplierResponse>(cancellationToken);

            return result!.Id;
        }
    }

    public sealed class ExactSupplierResponse
    {
        public string Id { get; set; } = string.Empty;
    }
}
