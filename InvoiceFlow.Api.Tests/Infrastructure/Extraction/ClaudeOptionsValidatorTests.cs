using InvoiceFlow.Api.Infrastructure.Extraction;

namespace InvoiceFlow.Api.Tests.Infrastructure.Extraction;

public sealed class ClaudeOptionsValidatorTests
{
    private readonly ClaudeOptionsValidator _validator = new();

    [Fact]
    public void Validate_ShouldSucceed_WhenModeIsDemo()
    {
        var options = new ClaudeOptions { Mode = "Demo", ApiKey = string.Empty };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenModeIsRealAndApiKeyIsProvided()
    {
        var options = new ClaudeOptions { Mode = "Real", ApiKey = "sk-ant-test" };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ShouldFail_WhenModeIsRealAndApiKeyIsEmpty()
    {
        var options = new ClaudeOptions { Mode = "Real", ApiKey = string.Empty };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_ShouldFail_WhenModeIsRealAndApiKeyIsWhitespace()
    {
        var options = new ClaudeOptions { Mode = "Real", ApiKey = "   " };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenModeIsUnrecognized()
    {
        var options = new ClaudeOptions { Mode = "Unknown", ApiKey = string.Empty };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }
}
