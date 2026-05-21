using InvoiceFlow.Api.Auth;
using InvoiceFlow.Api.Features.Exact;
using InvoiceFlow.Api.Features.Invoices.Extraction;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Features.Invoices.RetryExtraction;
using InvoiceFlow.Api.Features.Invoices.Review;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using InvoiceFlow.Api.Features.Suppliers.CreateSupplier;
using InvoiceFlow.Api.Features.Suppliers.Idempotency;
using InvoiceFlow.Api.Features.Suppliers.Matching;
using InvoiceFlow.Api.Infrastructure;
using InvoiceFlow.Api.Infrastructure.Background;
using InvoiceFlow.Api.Infrastructure.Exact;
using InvoiceFlow.Api.Infrastructure.Extraction;
using InvoiceFlow.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

var authEnabled = builder.Configuration.GetValue<bool>("Authentication:Enabled", true);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    if (authEnabled)
    {
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT Bearer token."
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    }
});

// Checks both "scope" and "scp" claim types, splitting space-separated values.
// This covers Auth0/Keycloak ("scope") and Entra ID ("scp") conventions.
static bool HasScope(ClaimsPrincipal user, string required) =>
    user.Claims
        .Where(c => c.Type is "scope" or "scp")
        .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        .Contains(required, StringComparer.Ordinal);

if (authEnabled)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = builder.Configuration["Jwt:Authority"];
            options.Audience = builder.Configuration["Jwt:Audience"];
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(PolicyNames.InvoiceReader, p =>
            p.RequireAuthenticatedUser()
             .RequireAssertion(ctx => HasScope(ctx.User, "invoiceflow:read")));

        options.AddPolicy(PolicyNames.InvoiceUploader, p =>
            p.RequireAuthenticatedUser()
             .RequireAssertion(ctx => HasScope(ctx.User, "invoiceflow:upload")));

        options.AddPolicy(PolicyNames.InvoiceReviewer, p =>
            p.RequireAuthenticatedUser()
             .RequireAssertion(ctx => HasScope(ctx.User, "invoiceflow:review")));

        options.AddPolicy(PolicyNames.InvoiceAdmin, p =>
            p.RequireAuthenticatedUser()
             .RequireAssertion(ctx => HasScope(ctx.User, "invoiceflow:admin")));
    });
}
else
{
    builder.Services.AddAuthentication();

    var passThrough = new AuthorizationPolicyBuilder()
        .RequireAssertion(_ => true)
        .Build();

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(PolicyNames.InvoiceReader,   passThrough);
        options.AddPolicy(PolicyNames.InvoiceUploader, passThrough);
        options.AddPolicy(PolicyNames.InvoiceReviewer, passThrough);
        options.AddPolicy(PolicyNames.InvoiceAdmin,    passThrough);
    });
}

builder.Services.AddSingleton<IInvoiceFolderReader, LocalInvoiceFolderReader>();
builder.Services.AddSingleton<IInvoiceTextExtractor, PdfPigTextExtractor>();

var claudeMode = builder.Configuration["Claude:Mode"] ?? "Demo";

if (claudeMode == "Real")
{
    builder.Services.AddOptions<ClaudeOptions>()
        .Bind(builder.Configuration.GetSection("Claude"))
        .ValidateOnStart();
    builder.Services.AddSingleton<IValidateOptions<ClaudeOptions>, ClaudeOptionsValidator>();
    builder.Services.AddSingleton<ClaudePromptBuilder>();
    builder.Services.AddHttpClient<ILlmInvoiceExtractor, ClaudeInvoiceExtractor>(client =>
    {
        client.BaseAddress = new Uri("https://api.anthropic.com/");
    });
}
else
{
    builder.Services.AddSingleton<ILlmInvoiceExtractor, DemoLlmInvoiceExtractor>();
}

builder.Services.AddScoped<IInvoiceParser, LlmInvoiceParser>();
builder.Services.AddScoped<ISupplierMatcher, MappingBasedSupplierMatcher>();
builder.Services.AddSingleton<IUploadedInvoiceFileStore, LocalUploadedInvoiceFileStore>();
builder.Services.AddSingleton<IUploadedInvoiceFileStore, LocalUploadedInvoiceFileStore>();
builder.Services.AddScoped<IInvoiceUploadService, InvoiceUploadService>();
builder.Services.AddScoped<IExactPostOutboxWriter, EfExactPostOutboxWriter>();
builder.Services.AddScoped<IExactInvoicePostingService, ExactInvoicePostingService>();
builder.Services.AddHostedService<ExactPostOutboxWorker>();
builder.Services.AddScoped<InvoiceParseResultValidator>();
builder.Services.AddScoped<SupplierCreateValidator>();
builder.Services.AddScoped<ISupplierCreateOutboxWriter, EfSupplierCreateOutboxWriter>();
builder.Services.AddScoped<ISupplierCreator, ExactSupplierCreator>();
builder.Services.AddHostedService<SupplierCreateWorker>();
builder.Services.AddScoped<SupplierFingerprintBuilder>();
builder.Services.AddScoped<BankAccountFingerprintBuilder>();
builder.Services.AddScoped<ISupplierMappingStore, EfSupplierMappingStore>();
builder.Services.AddScoped<IBankAccountMappingStore, EfBankAccountMappingStore>();
builder.Services.AddScoped<IBankDetailsRiskEvaluator, DefaultBankDetailsRiskEvaluator>();
builder.Services.AddScoped<IInvoiceReviewService, InvoiceReviewService>();
builder.Services.AddScoped<IInvoiceRetryService, InvoiceRetryService>();

builder.Services.AddDbContext<InvoiceFlowDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("InvoiceFlowDatabase"));
});

builder.Services.AddHttpClient<ISupplierCreator, ExactSupplierCreator>(client =>
{
    client.BaseAddress = new Uri("https://api.exactonline.com/");
});

builder.Services.Configure<ExactOptions>(
    builder.Configuration.GetSection("Exact"));

builder.Services.AddScoped<IUploadedInvoiceStore, EfUploadedInvoiceStore>();

var app = builder.Build();

if (authEnabled && !app.Environment.IsDevelopment())
{
    var authority = app.Configuration["Jwt:Authority"];
    var audience  = app.Configuration["Jwt:Audience"];
    if (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(audience))
    {
        throw new InvalidOperationException(
            "Jwt:Authority and Jwt:Audience must be configured when Authentication:Enabled is true.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
