namespace InvoiceFlow.Api.Features.Suppliers.Matching
{
    public sealed class BankDetailsRiskResult
    {
        public bool IsSafe { get; set; }
        public bool IsNewBankDetails { get; set; }
        public bool HasConflict { get; set; }
        public List<string> Reasons { get; set; } = new();
    }
}
