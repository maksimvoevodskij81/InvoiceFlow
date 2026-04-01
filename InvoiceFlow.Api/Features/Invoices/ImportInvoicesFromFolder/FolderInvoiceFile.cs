namespace InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder
{
    public sealed class FolderInvoiceFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
    }
}
