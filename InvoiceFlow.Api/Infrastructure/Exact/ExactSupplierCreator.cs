using InvoiceFlow.Api.Features.Suppliers.CreateSupplier;

namespace InvoiceFlow.Api.Infrastructure.Exact
{
    public sealed class ExactSupplierCreator : ISupplierCreator
    {
        public async Task<string> CreateAsync(SupplierCreateRequest request, CancellationToken cancellationToken = default)
        {
            // TODO: call Exact API

            // TEMP: simulate
            await Task.Delay(100, cancellationToken);

            return Guid.NewGuid().ToString();
        }
    }
}
