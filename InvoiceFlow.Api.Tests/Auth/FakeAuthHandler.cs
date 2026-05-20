using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Api.Tests.Auth;

public sealed class FakeAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public FakeAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    // Reads scopes from the Bearer token value as space-separated strings.
    // Example: "Authorization: Bearer invoiceflow:read invoiceflow:upload"
    // → two claims: scope=invoiceflow:read, scope=invoiceflow:upload
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.ToString();

        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing Bearer token."));
        }

        var token = authHeader["Bearer ".Length..].Trim();
        var scopes = token.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var claims = new List<Claim> { new(ClaimTypes.Name, "test-user") };
        foreach (var scope in scopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
