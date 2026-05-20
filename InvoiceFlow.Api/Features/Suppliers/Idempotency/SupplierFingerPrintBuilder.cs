using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using System.Text.RegularExpressions;

namespace InvoiceFlow.Api.Features.Suppliers.Idempotency;

public sealed class SupplierFingerprintBuilder
{
    public string Build(InvoiceParseResult invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        string name = NormalizeText(invoice.SupplierName);
        string postcode = NormalizeText(invoice.SupplierPostcode);
        string country = NormalizeText(invoice.SupplierCountry);

        return $"NAME:{name}|POSTCODE:{postcode}|COUNTRY:{country}";
    }

    public string BuildNamePostcode(InvoiceParseResult invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        string name = NormalizeText(invoice.SupplierName);
        string postcode = NormalizeText(invoice.SupplierPostcode);

        return $"NAME:{name}|POSTCODE:{postcode}";
    }

    public string BuildNameAddressPostcode(InvoiceParseResult invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        string name = NormalizeText(invoice.SupplierName);
        string addr = NormalizeText(invoice.SupplierAddressLine);
        string postcode = NormalizeText(invoice.SupplierPostcode);

        return $"NAME:{name}|ADDR:{addr}|POSTCODE:{postcode}";
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string upper = value.Trim().ToUpperInvariant();
        upper = Regex.Replace(upper, @"\s+", " ");

        return upper;
    }
}