using InvoiceFlow.Api.Features.Invoices.Extraction;

namespace InvoiceFlow.Api.Tests.Features.Invoices.Extraction;

public sealed class ClaudePromptBuilderTests
{
    private readonly ClaudePromptBuilder _builder = new();

    [Fact]
    public void Build_ShouldIncludeInvoiceText_InUserMessage()
    {
        const string invoiceText = "Supplier: Acme BV\nInvoice: INV-001\nTotal: €123.45";

        var prompt = _builder.Build(invoiceText);

        Assert.Contains(invoiceText, prompt.UserMessage);
    }

    [Fact]
    public void Build_ShouldReturnNonEmptySystemPrompt()
    {
        var prompt = _builder.Build("any text");

        Assert.False(string.IsNullOrWhiteSpace(prompt.SystemPrompt));
    }

    [Fact]
    public void Build_ShouldMentionExpectedJsonFields_InSystemPrompt()
    {
        var prompt = _builder.Build("any text");

        Assert.Contains("supplier_name", prompt.SystemPrompt);
        Assert.Contains("invoice_number", prompt.SystemPrompt);
        Assert.Contains("invoice_date", prompt.SystemPrompt);
        Assert.Contains("total_amount", prompt.SystemPrompt);
        Assert.Contains("currency", prompt.SystemPrompt);
    }

    [Fact]
    public void Build_ShouldNotThrow_WhenTextIsEmpty()
    {
        var prompt = _builder.Build(string.Empty);

        Assert.NotNull(prompt);
        Assert.False(string.IsNullOrWhiteSpace(prompt.SystemPrompt));
    }

    [Fact]
    public void Build_ShouldNotThrow_WhenTextIsWhitespace()
    {
        var prompt = _builder.Build("   ");

        Assert.NotNull(prompt);
        Assert.False(string.IsNullOrWhiteSpace(prompt.SystemPrompt));
    }

    [Fact]
    public void Build_ShouldPreserveSpecialCharacters_InUserMessage()
    {
        const string invoiceText = "Leverancier: Müller GmbH\nIBAN: NL91ABNA0417164300\nBTW: €1.234,56";

        var prompt = _builder.Build(invoiceText);

        Assert.Contains(invoiceText, prompt.UserMessage);
    }

    [Fact]
    public void Build_ShouldInstructStrictJsonResponse_InSystemPrompt()
    {
        var prompt = _builder.Build("any text");

        Assert.Contains("JSON", prompt.SystemPrompt);
    }

    [Fact]
    public void Build_ShouldInstructCopyBankAccountAsIs_InSystemPrompt()
    {
        var prompt = _builder.Build("any text");

        Assert.Contains("exactly as it appears", prompt.SystemPrompt);
    }

    [Fact]
    public void Build_ShouldInstructKvkAndVatAsDutchOnly_InSystemPrompt()
    {
        var prompt = _builder.Build("any text");

        Assert.Contains("Dutch", prompt.SystemPrompt);
    }

    [Fact]
    public void Build_ShouldInstructDoNotGuess_InSystemPrompt()
    {
        var prompt = _builder.Build("any text");

        Assert.Contains("Do not guess", prompt.SystemPrompt);
    }

    [Fact]
    public void Build_ShouldInstructCurrencyAsIsoCode_InSystemPrompt()
    {
        var prompt = _builder.Build("any text");

        Assert.Contains("ISO 4217", prompt.SystemPrompt);
    }

    [Fact]
    public void Build_ShouldWrapInvoiceTextWithInstruction_InUserMessage()
    {
        const string invoiceText = "Supplier: Acme BV";

        var prompt = _builder.Build(invoiceText);

        Assert.Contains("Extract invoice data from the following text", prompt.UserMessage);
        Assert.Contains(invoiceText, prompt.UserMessage);
    }
}
