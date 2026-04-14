using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Features.Suppliers.Matching;

namespace InvoiceFlow.Api.Infrastructure;

public sealed class FakeSupplierMatcher : ISupplierMatcher
{
    public Task<SupplierMatchResult> MatchAsync(
        InvoiceParseResult parseResult,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parseResult);
        var result = new SupplierMatchResult
        {
            IsMatched = true,
            RequiresReview = false,
            MatchedBy = "BankAccount",
            InternalSupplierId = "internal-supplier-001",
            ExactSupplierId = "exact-supplier-001",
            Message = "Supplier matched successfully."
        };

        return Task.FromResult(result);
    }
}