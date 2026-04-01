namespace InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

public interface IInvoiceFolderReader
{
    FolderInvoiceFile? GetNext(string folderPath);
    FolderInvoiceFile? TakeNext(string folderPath);
}
