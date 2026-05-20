using InvoiceFlow.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

namespace InvoiceFlow.Api.Tests.Auth;

public sealed class AuthorizationTests : IClassFixture<AuthorizationTests.AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public AuthorizationTests(AuthTestFactory factory)
    {
        _factory = factory;
    }

    // --- 401: no token ---

    [Theory]
    [InlineData("/api/invoices")]
    [InlineData("/api/invoices/some-id")]
    [InlineData("/api/invoices/some-id/status")]
    public async Task GetEndpoints_NoToken_Returns401(string url)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Upload_NoToken_Returns401()
    {
        var client = _factory.CreateClient();

        using var content = new MultipartFormDataContent();
        var response = await client.PostAsync("/api/invoices/upload", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApproveReview_NoToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/invoices/some-id/review/approve", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- 403: authenticated but wrong scope ---

    [Fact]
    public async Task GetInvoices_WrongScope_Returns403()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invoiceflow:upload");

        var response = await client.GetAsync("/api/invoices");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ApproveReview_WrongScope_Returns403()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invoiceflow:read");

        var response = await client.PostAsync("/api/invoices/some-id/review/approve", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- business logic reached (not 401/403) ---

    [Fact]
    public async Task GetInvoices_CorrectScope_ReachesBusinessLogic()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invoiceflow:read");

        var response = await client.GetAsync("/api/invoices");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ApproveReview_CorrectScope_ReachesBusinessLogic()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invoiceflow:review");

        // 404 expected — invoice doesn't exist, but auth passed
        var response = await client.PostAsync("/api/invoices/nonexistent-id/review/approve", null);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    public sealed class AuthTestFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Enable real scope policies, but swap JwtBearer with FakeAuthHandler
            builder.UseSetting("Authentication:Enabled", "true");

            builder.ConfigureServices(services =>
            {
                // Override the default auth scheme with the fake handler.
                // ConfigureWebHost runs after Program.cs, so this wins.
                services.AddAuthentication(FakeAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>(
                        FakeAuthHandler.SchemeName, _ => { });

                var dbDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<InvoiceFlowDbContext>));
                if (dbDescriptor is not null)
                {
                    services.Remove(dbDescriptor);
                }
                services.AddDbContext<InvoiceFlowDbContext>(o =>
                    o.UseInMemoryDatabase("AuthTestDb"));
            });
        }
    }
}
