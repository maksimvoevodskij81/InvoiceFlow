namespace InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

public interface IInvoiceParser
{
    Task ParseAsync(FolderInvoiceFile file, CancellationToken cancellationToken = default);
}