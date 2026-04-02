using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

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
            MatchedBy = "IBAN",
            InternalSupplierId = "SUP-001",
            ExactSupplierId = "EXACT-001",
            Message = "Supplier matched successfully."
        };

        return Task.FromResult(result);
    }
}