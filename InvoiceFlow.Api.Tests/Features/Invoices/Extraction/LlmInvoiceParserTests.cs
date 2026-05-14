using InvoiceFlow.Api.Features.Invoices.Extraction;
using InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder;
using InvoiceFlow.Api.Infrastructure;

namespace InvoiceFlow.Api.Tests.Features.Invoices.Extraction;

public sealed class LlmInvoiceParserTests
{
    private readonly MockLlmInvoiceExtractor _mockExtractor;
    private readonly LlmInvoiceParser _parser;

    public LlmInvoiceParserTests()
    {
        _mockExtractor = new MockLlmInvoiceExtractor();
        _parser = new LlmInvoiceParser(_mockExtractor);
    }

    [Fact]
    public async Task ParseAsync_ShouldMapAllFields_WhenExtractionSucceeds()
    {
        // Arrange
        var file = new FolderInvoiceFile
        {
            FileName = "test.pdf",
            FullPath = "/test/test.pdf",
            ContentType = "application/pdf"
        };

        var extractedFields = new LlmExtractedFields
        {
            SupplierName = "Test Supplier",
            InvoiceNumber = "INV-123",
            InvoiceDate = new DateOnly(2026, 5, 1),
            TotalAmount = 500.00m,
            Currency = "EUR",
            SupplierAddressLine = "123 Main St",
            SupplierPostcode = "12345",
            SupplierCity = "Amsterdam",
            SupplierCountry = "NL",
            SupplierBankAccount = "NL91ABNA0417164300",
            SupplierBicCode = "ABNANL2A",
            SupplierVatNumber = "NL123456789B01",
            SupplierKvKNumber = "12345678"
        };

        _mockExtractor.SetSuccessfulResult(extractedFields);

        // Act
        var result = await _parser.ParseAsync(file);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Supplier", result.SupplierName);
        Assert.Equal("INV-123", result.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 5, 1), result.InvoiceDate);
        Assert.Equal(500.00m, result.TotalAmount);
        Assert.Equal("EUR", result.Currency);
        Assert.Equal("123 Main St", result.SupplierAddressLine);
        Assert.Equal("12345", result.SupplierPostcode);
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
        // Arrange
        var file = new FolderInvoiceFile
        {
            FileName = "test.pdf",
            FullPath = "/test/test.pdf",
            ContentType = "application/pdf"
        };

        _mockExtractor.SetFailedResult("MODEL_ERROR", "Model failed to extract");

        // Act
        var result = await _parser.ParseAsync(file);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.SupplierName);
        Assert.Equal(string.Empty, result.InvoiceNumber);
        Assert.Null(result.InvoiceDate);
        Assert.Null(result.TotalAmount);
        Assert.Equal(string.Empty, result.Currency);
    }

    [Fact]
    public async Task ParseAsync_ShouldReturnEmptyResult_WhenFieldsAreNull()
    {
        // Arrange
        var file = new FolderInvoiceFile
        {
            FileName = "test.pdf",
            FullPath = "/test/test.pdf",
            ContentType = "application/pdf"
        };

        _mockExtractor.SetResultWithNullFields();

        // Act
        var result = await _parser.ParseAsync(file);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.SupplierName);
        Assert.Equal(string.Empty, result.InvoiceNumber);
        Assert.Null(result.InvoiceDate);
    }

    [Fact]
    public async Task ParseAsync_ShouldStoreLastExtractionResult_AfterSuccessfulParse()
    {
        // Arrange
        var file = new FolderInvoiceFile
        {
            FileName = "test.pdf",
            FullPath = "/test/test.pdf",
            ContentType = "application/pdf"
        };

        var extractedFields = new LlmExtractedFields
        {
            SupplierName = "Test Supplier",
            InvoiceNumber = "INV-456"
        };

        _mockExtractor.SetSuccessfulResult(extractedFields);

        // Act
        await _parser.ParseAsync(file);

        // Assert
        Assert.NotNull(_parser.LastExtractionResult);
        Assert.True(_parser.LastExtractionResult.IsSuccessful);
        Assert.NotNull(_parser.LastExtractionResult.Fields);
        Assert.Equal("Test Supplier", _parser.LastExtractionResult.Fields.SupplierName);
    }

    [Fact]
    public async Task ParseAsync_ShouldStoreLastExtractionResult_AfterFailedParse()
    {
        // Arrange
        var file = new FolderInvoiceFile
        {
            FileName = "test.pdf",
            FullPath = "/test/test.pdf",
            ContentType = "application/pdf"
        };

        _mockExtractor.SetFailedResult("API_ERROR", "API timeout");

        // Act
        await _parser.ParseAsync(file);

        // Assert
        Assert.NotNull(_parser.LastExtractionResult);
        Assert.False(_parser.LastExtractionResult.IsSuccessful);
        Assert.NotNull(_parser.LastExtractionResult.Metadata.Error);
        Assert.Equal("API_ERROR", _parser.LastExtractionResult.Metadata.Error.Code);
    }

    [Fact]
    public async Task ParseAsync_ShouldMapPartialFields_WhenSomeFieldsAreNull()
    {
        // Arrange
        var file = new FolderInvoiceFile
        {
            FileName = "test.pdf",
            FullPath = "/test/test.pdf",
            ContentType = "application/pdf"
        };

        var extractedFields = new LlmExtractedFields
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

        _mockExtractor.SetSuccessfulResult(extractedFields);

        // Act
        var result = await _parser.ParseAsync(file);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Supplier", result.SupplierName);
        Assert.Equal(string.Empty, result.InvoiceNumber); // Null mapped to empty
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
        // Arrange
        var file = new FolderInvoiceFile
        {
            FileName = "test.pdf",
            FullPath = "/test/test.pdf",
            ContentType = "application/pdf"
        };

        _mockExtractor.SetToThrow(new InvalidOperationException("Unexpected error"));

        // Act & Assert - should not throw
        var result = await _parser.ParseAsync(file);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.SupplierName);
        Assert.Null(_parser.LastExtractionResult?.Fields);
        Assert.False(_parser.LastExtractionResult?.IsSuccessful ?? true);
        Assert.NotNull(_parser.LastExtractionResult?.Metadata.Error);
        Assert.Equal("EXTRACTION_EXCEPTION", _parser.LastExtractionResult.Metadata.Error.Code);
    }
}

internal sealed class MockLlmInvoiceExtractor : ILlmInvoiceExtractor
{
    private LlmExtractionResult? _resultToReturn;
    private Exception? _exceptionToThrow;

    public void SetSuccessfulResult(LlmExtractedFields fields)
    {
        _resultToReturn = new LlmExtractionResult
        {
            IsSuccessful = true,
            Raw = new LlmRawExtractionResult
            {
                RawJson = null,
                ModelName = "Mock-LLM",
                ExtractedAtUtc = DateTime.UtcNow,
                IsSuccessful = true,
                ErrorMessage = null
            },
            Fields = fields,
            Metadata = new ExtractionMetadata
            {
                ModelName = "Mock-LLM",
                StartedAtUtc = DateTime.UtcNow,
                CompletedAtUtc = DateTime.UtcNow,
                IsSuccessful = true,
                Warnings = new(),
                Error = null
            }
        };
        _exceptionToThrow = null;
    }

    public void SetFailedResult(string errorCode, string errorMessage)
    {
        _resultToReturn = new LlmExtractionResult
        {
            IsSuccessful = false,
            Raw = new LlmRawExtractionResult
            {
                RawJson = null,
                ModelName = "Mock-LLM",
                ExtractedAtUtc = DateTime.UtcNow,
                IsSuccessful = false,
                ErrorMessage = errorMessage
            },
            Fields = null,
            Metadata = new ExtractionMetadata
            {
                ModelName = "Mock-LLM",
                StartedAtUtc = DateTime.UtcNow,
                CompletedAtUtc = DateTime.UtcNow,
                IsSuccessful = false,
                Warnings = new(),
                Error = new ExtractionError
                {
                    Code = errorCode,
                    Message = errorMessage,
                    IsRetryable = true
                }
            }
        };
        _exceptionToThrow = null;
    }

    public void SetResultWithNullFields()
    {
        _resultToReturn = new LlmExtractionResult
        {
            IsSuccessful = true,
            Raw = new LlmRawExtractionResult
            {
                RawJson = null,
                ModelName = "Mock-LLM",
                ExtractedAtUtc = DateTime.UtcNow,
                IsSuccessful = true,
                ErrorMessage = null
            },
            Fields = null,
            Metadata = new ExtractionMetadata
            {
                ModelName = "Mock-LLM",
                StartedAtUtc = DateTime.UtcNow,
                CompletedAtUtc = DateTime.UtcNow,
                IsSuccessful = true,
                Warnings = new(),
                Error = null
            }
        };
        _exceptionToThrow = null;
    }

    public void SetToThrow(Exception exception)
    {
        _exceptionToThrow = exception;
        _resultToReturn = null;
    }

    public Task<LlmExtractionResult> ExtractAsync(FolderInvoiceFile file, CancellationToken cancellationToken = default)
    {
        if (_exceptionToThrow != null)
        {
            throw _exceptionToThrow;
        }

        if (_resultToReturn != null)
        {
            return Task.FromResult(_resultToReturn);
        }

        throw new InvalidOperationException("Mock extractor not configured");
    }
}
