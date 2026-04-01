using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;


namespace InvoiceFlow.Api.Infrastructure;

public sealed class FakeInvoiceParser : IInvoiceParser
{
    public Task<InvoiceParseResult> ParseAsync(FolderInvoiceFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        var result = new InvoiceParseResult
        {
            SupplierName = "Demo Supplier",
            InvoiceNumber = "INV-001",
            InvoiceDate = new DateOnly(2026, 4, 1),
            TotalAmount = 123.45m,
            Currency = "EUR"
        };

        return Task.FromResult(result);
    }
}