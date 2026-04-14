namespace InvoiceFlow.Api.Features.Suppliers.Matching
{
    public enum SupplierMatchDecisionType
    {
        Matched,
        MatchedWithNewBankDetails,
        CanCreateSupplier,
        Review,
        NoMatch
    }

    public sealed class SupplierMatchDecision
    {
        public SupplierMatchDecisionType Decision { get; set; }
        public string? InternalSupplierId { get; set; }
        public string? ExactSupplierId { get; set; }
        public List<string> Reasons { get; set; } = new();
    }
}
