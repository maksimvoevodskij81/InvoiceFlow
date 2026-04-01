using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;


namespace InvoiceFlow.Api.Infrastructure;

public sealed class FakeInvoiceParser : IInvoiceParser
{
    public Task ParseAsync(FolderInvoiceFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        return Task.CompletedTask;
    }
}