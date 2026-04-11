namespace InvoiceFlow.Api.Features.Invoices.UploadInvoice
{
    public sealed class UploadDecision
    {
        public bool IsValid { get; init; }
        public bool ReadyToPost { get; init; }
        public bool CanCreateSupplier { get; init; }

        public List<string> MissingFields { get; init; } = new();

        public Guid? SupplierId { get; init; }
    }
}
