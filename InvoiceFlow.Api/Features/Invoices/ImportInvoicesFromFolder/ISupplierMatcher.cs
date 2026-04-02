namespace InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

public interface ISupplierMatcher
{
    Task<SupplierMatchResult> MatchAsync(
        InvoiceParseResult parseResult,
        CancellationToken cancellationToken = default);
}