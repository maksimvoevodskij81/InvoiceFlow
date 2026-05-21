using InvoiceFlow.Api.Contracts;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using Microsoft.AspNetCore.Http;

namespace InvoiceFlow.Api.Tests.Fakes;

public sealed class FakeInvoiceUploadService : IInvoiceUploadService
{
    public int CallsCount { get; private set; }

    public IFormFile? LastFile { get; private set; }

    public string? LastUploadedBy { get; private set; }

    public UploadInvoiceAcceptedResponse Response { get; set; } = new UploadInvoiceAcceptedResponse
    {
        InvoiceId = "123",
        Status = InvoiceStatuses.Parsed,
        Message = "Invoice parsed successfully."
    };

    public Task<UploadInvoiceAcceptedResponse> UploadAsync(
        IFormFile file,
        string? uploadedBy = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        CallsCount++;
        LastFile = file;
        LastUploadedBy = uploadedBy;

        return Task.FromResult(Response);
    }
}