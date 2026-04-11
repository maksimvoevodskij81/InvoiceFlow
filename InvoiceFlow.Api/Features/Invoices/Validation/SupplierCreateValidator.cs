using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

namespace InvoiceFlow.Api.Features.Invoices.UploadInvoice;

public sealed class SupplierCreateValidator
{
    public List<string> Validate(InvoiceParseResult invoice)
    {
        List<string> missing = new();

        if (string.IsNullOrWhiteSpace(invoice.SupplierName))
        {
            missing.Add(nameof(invoice.SupplierName));
        }

        if (string.IsNullOrWhiteSpace(invoice.SupplierAddressLine))
        {
            missing.Add(nameof(invoice.SupplierAddressLine));
        }

        if (string.IsNullOrWhiteSpace(invoice.SupplierPostcode))
        {
            missing.Add(nameof(invoice.SupplierPostcode));
        }

        if (string.IsNullOrWhiteSpace(invoice.SupplierCity))
        {
            missing.Add(nameof(invoice.SupplierCity));
        }

        if (string.IsNullOrWhiteSpace(invoice.SupplierCountry))
        {
            missing.Add(nameof(invoice.SupplierCountry));
        }

        return missing;
    }
}