namespace InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

public interface IInvoiceParser
{
    Task<InvoiceParseResult> ParseAsync(FolderInvoiceFile file, CancellationToken cancellationToken = default);
}