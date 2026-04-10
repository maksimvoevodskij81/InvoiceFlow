using InvoiceFlow.Api.Features.Exact;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Features.Invoices.UploadInvoice;
using InvoiceFlow.Api.Infrastructure;
using InvoiceFlow.Api.Infrastructure.Background;
using InvoiceFlow.Api.Infrastructure.Exact;
using InvoiceFlow.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IInvoiceFolderReader, LocalInvoiceFolderReader>();
builder.Services.AddSingleton<IInvoiceParser, FakeInvoiceParser>();
builder.Services.AddSingleton<ISupplierMatcher, FakeSupplierMatcher>();
builder.Services.AddSingleton<IUploadedInvoiceFileStore, LocalUploadedInvoiceFileStore>();
builder.Services.AddSingleton<IUploadedInvoiceFileStore, LocalUploadedInvoiceFileStore>();
builder.Services.AddScoped<IInvoiceUploadService, InvoiceUploadService>();
builder.Services.AddScoped<IExactPostOutboxWriter, EfExactPostOutboxWriter>();
builder.Services.AddScoped<IExactInvoicePostingService, FakeExactInvoicePostingService>();
builder.Services.AddHostedService<ExactPostOutboxWorker>();

builder.Services.AddDbContext<InvoiceFlowDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("InvoiceFlowDatabase"));
});

builder.Services.AddScoped<IUploadedInvoiceStore, EfUploadedInvoiceStore>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
