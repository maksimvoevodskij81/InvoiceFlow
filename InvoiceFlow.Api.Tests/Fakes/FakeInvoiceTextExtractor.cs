using InvoiceFlow.Api.Features.Invoices.Extraction;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

namespace InvoiceFlow.Api.Tests.Fakes;

public sealed class FakeInvoiceTextExtractor : IInvoiceTextExtractor
{
    public string Result { get; set; } = "Sample invoice text";

    public Task<string> ExtractTextAsync(FolderInvoiceFile file, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result);
    }
}
