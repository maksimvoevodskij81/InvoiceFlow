using InvoiceFlow.Api.Features.Invoices.Extraction;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;

namespace InvoiceFlow.Api.Tests.Features.Invoices.Extraction;

public sealed class LlmInvoiceParserTests
{
    private static readonly FolderInvoiceFile AnyFile = new()
    {
        FileName = "invoice.pdf",
        FullPath = "C:\\invoices\\invoice.pdf",
        ContentType = "application/pdf"
    };

    [Fact]
    public async Task ParseAsync_ShouldMapAllFields_WhenExtractionSucceeds()
    {
        var fields = new LlmExtractedFields
        {
            SupplierName = "Acme BV",
            InvoiceNumber = "INV-2026-001",
            InvoiceDate = new DateOnly(2026, 3, 15),
            TotalAmount = 1250.00m,
            Currency = "EUR",
            SupplierAddressLine = "Hoofdstraat 1",
            SupplierPostcode = "1234AB",
            SupplierCity = "Amsterdam",
            SupplierCountry = "NL",
            SupplierBankAccount = "NL91ABNA0417164300",
            SupplierBicCode = "ABNANL2A",
            SupplierVatNumber = "NL123456789B01",
            SupplierKvKNumber = "12345678"
        };

        var extractor = new StubLlmInvoiceExtractor(SuccessResult(fields));
        var parser = new LlmInvoiceParser(extractor);

        var result = await parser.ParseAsync(AnyFile);

        Assert.Equal("Acme BV", result.SupplierName);
        Assert.Equal("INV-2026-001", result.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 3, 15), result.InvoiceDate);
        Assert.Equal(1250.00m, result.TotalAmount);
        Assert.Equal("EUR", result.Currency);
        Assert.Equal("Hoofdstraat 1", result.SupplierAddressLine);
        Assert.Equal("1234AB", result.SupplierPostcode);
        Assert.Equal("Amsterdam", result.SupplierCity);
        Assert.Equal("NL", result.SupplierCountry);
        Assert.Equal("NL91ABNA0417164300", result.SupplierBankAccount);
        Assert.Equal("ABNANL2A", result.SupplierBicCode);
        Assert.Equal("NL123456789B01", result.SupplierVatNumber);
        Assert.Equal("12345678", result.SupplierKvKNumber);
    }

    [Fact]
    public async Task ParseAsync_ShouldReturnEmptyResult_WhenExtractionFails()
    {
        var extractor = new StubLlmInvoiceExtractor(FailureResult("MalformedJson", "Could not parse LLM response."));
        var parser = new LlmInvoiceParser(extractor);

        var result = await parser.ParseAsync(AnyFile);

        Assert.Equal(string.Empty, result.SupplierName);
        Assert.Equal(string.Empty, result.InvoiceNumber);
        Assert.Equal(string.Empty, result.Currency);
        Assert.Null(result.InvoiceDate);
        Assert.Null(result.TotalAmount);
    }

    [Fact]
    public async Task ParseAsync_ShouldReturnEmptyResult_WhenFieldsAreNull()
    {
        var extractionResult = new LlmExtractionResult
        {
            IsSuccessful = true,
            Raw = new LlmRawExtractionResult { RawJson = "{}" },
            Fields = null,
            Metadata = new ExtractionMetadata { Model = "demo", ExtractedAtUtc = DateTime.UtcNow, Warnings = [] }
        };

        var extractor = new StubLlmInvoiceExtractor(extractionResult);
        var parser = new LlmInvoiceParser(extractor);

        var result = await parser.ParseAsync(AnyFile);

        Assert.Equal(string.Empty, result.SupplierName);
        Assert.Equal(string.Empty, result.InvoiceNumber);
        Assert.Equal(string.Empty, result.Currency);
        Assert.Null(result.InvoiceDate);
        Assert.Null(result.TotalAmount);
    }

    [Fact]
    public async Task ParseAsync_ShouldStoreLastExtractionResult_AfterSuccessfulParse()
    {
        var fields = new LlmExtractedFields
        {
            SupplierName = "Test BV",
            InvoiceNumber = "INV-999",
            Currency = "USD"
        };

        var extractor = new StubLlmInvoiceExtractor(SuccessResult(fields));
        var parser = new LlmInvoiceParser(extractor);

        await parser.ParseAsync(AnyFile);

        Assert.NotNull(parser.LastExtractionResult);
        Assert.True(parser.LastExtractionResult!.IsSuccessful);
        Assert.NotNull(parser.LastExtractionResult.Fields);
        Assert.Equal("Test BV", parser.LastExtractionResult.Fields!.SupplierName);
    }

    [Fact]
    public async Task ParseAsync_ShouldStoreLastExtractionResult_AfterFailedParse()
    {
        var extractor = new StubLlmInvoiceExtractor(FailureResult("Timeout", "Extraction timed out."));
        var parser = new LlmInvoiceParser(extractor);

        await parser.ParseAsync(AnyFile);

        Assert.NotNull(parser.LastExtractionResult);
        Assert.False(parser.LastExtractionResult!.IsSuccessful);
        Assert.NotNull(parser.LastExtractionResult.Error);
        Assert.Equal("Timeout", parser.LastExtractionResult.Error!.Code);
    }

    [Fact]
    public async Task ParseAsync_ShouldMapPartialFields_WhenSomeFieldsAreNull()
    {
        var fields = new LlmExtractedFields
        {
            SupplierName = "Test Supplier",
            InvoiceNumber = null,
            InvoiceDate = new DateOnly(2026, 5, 1),
            TotalAmount = null,
            Currency = "EUR",
            SupplierAddressLine = "456 Oak Ave",
            SupplierPostcode = null,
            SupplierCity = "Rotterdam",
            SupplierCountry = null
        };

        var extractor = new StubLlmInvoiceExtractor(SuccessResult(fields));
        var parser = new LlmInvoiceParser(extractor);

        var result = await parser.ParseAsync(AnyFile);

        Assert.Equal("Test Supplier", result.SupplierName);
        Assert.Equal(string.Empty, result.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 5, 1), result.InvoiceDate);
        Assert.Null(result.TotalAmount);
        Assert.Equal("EUR", result.Currency);
        Assert.Equal("456 Oak Ave", result.SupplierAddressLine);
        Assert.Null(result.SupplierPostcode);
        Assert.Equal("Rotterdam", result.SupplierCity);
        Assert.Null(result.SupplierCountry);
    }

    [Fact]
    public async Task ParseAsync_ShouldNotThrow_WhenExtractorThrowsUnexpectedly()
    {
        var extractor = new ThrowingLlmInvoiceExtractor("Unexpected internal error.");
        var parser = new LlmInvoiceParser(extractor);

        var result = await parser.ParseAsync(AnyFile);

        Assert.Equal(string.Empty, result.SupplierName);
        Assert.NotNull(parser.LastExtractionResult);
        Assert.False(parser.LastExtractionResult!.IsSuccessful);
        Assert.NotNull(parser.LastExtractionResult.Error);
        Assert.Equal("UnexpectedException", parser.LastExtractionResult.Error!.Code);
        Assert.Equal("Unexpected internal error.", parser.LastExtractionResult.Error.Message);
    }

    private static LlmExtractionResult SuccessResult(LlmExtractedFields fields)
    {
        return new LlmExtractionResult
        {
            IsSuccessful = true,
            Raw = new LlmRawExtractionResult { RawJson = "{}" },
            Fields = fields,
            Metadata = new ExtractionMetadata
            {
                Model = "demo",
                ExtractedAtUtc = DateTime.UtcNow,
                Warnings = []
            }
        };
    }

    private static LlmExtractionResult FailureResult(string code, string message)
    {
        return new LlmExtractionResult
        {
            IsSuccessful = false,
            Raw = new LlmRawExtractionResult { RawJson = null },
            Fields = null,
            Metadata = new ExtractionMetadata
            {
                Model = "demo",
                ExtractedAtUtc = DateTime.UtcNow,
                Warnings = []
            },
            Error = new ExtractionError { Code = code, Message = message }
        };
    }
}

file sealed class StubLlmInvoiceExtractor : ILlmInvoiceExtractor
{
    private readonly LlmExtractionResult _result;

    public StubLlmInvoiceExtractor(LlmExtractionResult result)
    {
        _result = result;
    }

    public Task<LlmExtractionResult> ExtractAsync(FolderInvoiceFile file, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_result);
    }
}

file sealed class ThrowingLlmInvoiceExtractor : ILlmInvoiceExtractor
{
    private readonly string _message;

    public ThrowingLlmInvoiceExtractor(string message)
    {
        _message = message;
    }

    public Task<LlmExtractionResult> ExtractAsync(FolderInvoiceFile file, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(_message);
    }
}
