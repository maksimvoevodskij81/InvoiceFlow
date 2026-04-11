using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

namespace InvoiceFlow.Api.Features.Invoices.UploadInvoice;

public sealed class InvoiceParseResultValidator
{
    public List<string> Validate(InvoiceParseResult parseResult)
    {
        ArgumentNullException.ThrowIfNull(parseResult);

        List<string> missingFields = new();

        if (string.IsNullOrWhiteSpace(parseResult.SupplierName))
        {
            missingFields.Add(nameof(parseResult.SupplierName));
        }

        if (string.IsNullOrWhiteSpace(parseResult.InvoiceNumber))
        {
            missingFields.Add(nameof(parseResult.InvoiceNumber));
        }

        if (!parseResult.InvoiceDate.HasValue)
        {
            missingFields.Add(nameof(parseResult.InvoiceDate));
        }

        if (!parseResult.TotalAmount.HasValue)
        {
            missingFields.Add(nameof(parseResult.TotalAmount));
        }

        if (string.IsNullOrWhiteSpace(parseResult.Currency))
        {
            missingFields.Add(nameof(parseResult.Currency));
        }

        return missingFields;
    }
}