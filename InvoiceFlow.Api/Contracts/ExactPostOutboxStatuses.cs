namespace InvoiceFlow.Api.Contracts;

public static class ExactPostOutboxStatuses
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Posted = "Posted";
    public const string Failed = "Failed";
}