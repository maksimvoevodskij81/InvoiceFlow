using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Features.Suppliers.Idempotency;

namespace InvoiceFlow.Api.Features.Suppliers.Matching;

public sealed class DefaultBankDetailsRiskEvaluator : IBankDetailsRiskEvaluator
{
    private readonly IBankAccountMappingStore _bankAccountMappingStore;
    private readonly BankAccountFingerprintBuilder _bankAccountFingerprintBuilder;

    public DefaultBankDetailsRiskEvaluator(
        IBankAccountMappingStore bankAccountMappingStore,
        BankAccountFingerprintBuilder bankAccountFingerprintBuilder)
    {
        _bankAccountMappingStore = bankAccountMappingStore;
        _bankAccountFingerprintBuilder = bankAccountFingerprintBuilder;
    }

    public async Task<BankDetailsRiskResult> EvaluateAsync(
        InvoiceParseResult parseResult,
        string? matchedExactSupplierId,
        CancellationToken cancellationToken = default)
    {
        var result = new BankDetailsRiskResult
        {
            IsSafe = true
        };

        var bankFingerprint = _bankAccountFingerprintBuilder.Build(parseResult.SupplierBankAccount);

        if (string.IsNullOrWhiteSpace(bankFingerprint) || string.IsNullOrWhiteSpace(matchedExactSupplierId))
        {
            return result;
        }

        var existingOwner = await _bankAccountMappingStore.FindExactSupplierIdAsync(
            bankFingerprint,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(existingOwner))
        {
            result.IsSafe = false;
            result.IsNewBankDetails = true;
            result.Reasons.Add("Bank account is new for the matched supplier.");
            return result;
        }

        if (!string.Equals(existingOwner, matchedExactSupplierId, StringComparison.Ordinal))
        {
            result.IsSafe = false;
            result.HasConflict = true;
            result.Reasons.Add("Bank account is already linked to another supplier.");
            return result;
        }

        result.Reasons.Add("Bank account matches the existing supplier.");
        return result;
    }
}