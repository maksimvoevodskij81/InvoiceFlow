using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Features.Suppliers.Matching;

namespace InvoiceFlow.Api.Tests.Fakes;

public sealed class FakeBankDetailsRiskEvaluator : IBankDetailsRiskEvaluator
{
    public BankDetailsRiskResult Result { get; set; } = new()
    {
        IsSafe = true
    };

    public Task<BankDetailsRiskResult> EvaluateAsync(
        InvoiceParseResult parseResult,
        string exactSupplierId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result);
    }
}