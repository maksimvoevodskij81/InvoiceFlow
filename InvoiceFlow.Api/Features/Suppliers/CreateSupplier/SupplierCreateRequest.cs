namespace InvoiceFlow.Api.Features.Suppliers.CreateSupplier
{
    public sealed class SupplierCreateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string Postcode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }
}
