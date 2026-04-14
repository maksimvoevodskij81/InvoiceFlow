using InvoiceFlow.Api.Features.Invoices;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Features.Suppliers.Matching;

namespace InvoiceFlow.Api.Infrastructure;

public sealed class FakeSupplierMatcher : ISupplierMatcher
{
    public SupplierMatchResult Result { get; set; } = new()
    {
        IsMatched = true,
        RequiresReview = false,
        MatchedBy = SupplierMatchSources.BankAccount,
        InternalSupplierId = "internal-supplier-001",
        ExactSupplierId = "exact-supplier-001",
        Message = "Supplier matched successfully."
    };

    public Task<SupplierMatchResult> MatchAsync(
        InvoiceParseResult parseResult,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parseResult);

        return Task.FromResult(Result);
    }
}