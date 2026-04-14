using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

namespace InvoiceFlow.Api.Features.Suppliers.Matching
{
    public interface IBankDetailsRiskEvaluator
    {
        Task<BankDetailsRiskResult> EvaluateAsync(
            InvoiceParseResult parseResult,
            string? matchedExactSupplierId,
            CancellationToken cancellationToken = default);
    }
}
