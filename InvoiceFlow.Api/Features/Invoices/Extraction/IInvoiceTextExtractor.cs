using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

namespace InvoiceFlow.Api.Features.Invoices.Extraction;

public interface IInvoiceTextExtractor
{
    Task<string> ExtractTextAsync(FolderInvoiceFile file, CancellationToken cancellationToken = default);
}
