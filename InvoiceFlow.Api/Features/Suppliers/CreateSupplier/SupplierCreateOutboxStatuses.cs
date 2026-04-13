namespace InvoiceFlow.Api.Features.Suppliers.CreateSupplier;

public static class SupplierCreateOutboxStatuses
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
}