using Microsoft.Extensions.Options;

namespace InvoiceFlow.Api.Infrastructure.Extraction;

public sealed class ClaudeOptionsValidator : IValidateOptions<ClaudeOptions>
{
    public ValidateOptionsResult Validate(string? name, ClaudeOptions options)
    {
        if (options.Mode == "Real" && string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return ValidateOptionsResult.Fail(
                "Claude:ApiKey must be configured when Claude:Mode is 'Real'.");
        }

        return ValidateOptionsResult.Success;
    }
}
