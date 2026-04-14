namespace InvoiceFlow.Api.Features.Suppliers.Matching
{
    public sealed class SupplierMatchResult
    {
        public bool IsMatched { get; set; }
        public bool RequiresReview { get; set; }
        public bool CanCreateSupplier { get; set; }
        public bool HasNewBankDetails { get; set; }

        public string? MatchedBy { get; set; }
        public string? InternalSupplierId { get; set; }
        public string? ExactSupplierId { get; set; }
        public string? Message { get; set; }

        public List<string> Reasons { get; set; } = new();
    }
}
