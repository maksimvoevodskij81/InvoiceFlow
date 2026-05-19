using InvoiceFlow.Api.Features.Invoices.Extraction;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Infrastructure.Extraction;
using InvoiceFlow.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Api.Tests;

public sealed class StartupSmokeTests : IClassFixture<StartupSmokeTests.DemoModeFactory>
{
    private readonly DemoModeFactory _factory;

    public StartupSmokeTests(DemoModeFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Application_ShouldResolveILlmInvoiceExtractor_AsDemoExtractor_InDemoMode()
    {
        using var scope = _factory.Services.CreateScope();

        var extractor = scope.ServiceProvider.GetRequiredService<ILlmInvoiceExtractor>();

        Assert.IsType<DemoLlmInvoiceExtractor>(extractor);
    }

    [Fact]
    public void Application_ShouldResolveIInvoiceParser_AsLlmInvoiceParser_InDemoMode()
    {
        using var scope = _factory.Services.CreateScope();

        var parser = scope.ServiceProvider.GetRequiredService<IInvoiceParser>();

        Assert.IsType<LlmInvoiceParser>(parser);
    }

    [Fact]
    public void Application_ShouldResolveIInvoiceTextExtractor_AsPdfPigTextExtractor_InDemoMode()
    {
        using var scope = _factory.Services.CreateScope();

        var textExtractor = scope.ServiceProvider.GetRequiredService<IInvoiceTextExtractor>();

        Assert.IsType<PdfPigTextExtractor>(textExtractor);
    }

    public sealed class DemoModeFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Claude:Mode", "Demo");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<InvoiceFlowDbContext>));

                if (descriptor is not null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<InvoiceFlowDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
            });
        }
    }
}
