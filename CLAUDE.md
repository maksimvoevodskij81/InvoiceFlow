# InvoiceFlow — Claude Code Instructions

## Project overview

InvoiceFlow is a .NET invoice processing system.

Current workflow:

```text
upload invoice file
→ duplicate check by file hash
→ save file
→ parse invoice via IInvoiceParser
→ validate required fields
→ supplier matching
→ bank details risk evaluation
→ decide status
→ enqueue supplier creation or Exact posting
→ manual review approve/reject if needed

The project is evolving toward LLM-based invoice extraction, but the existing workflow must stay stable.

Main architectural rule

Do not rewrite the whole workflow.

Prefer small, safe, PR-sized changes.

Рус: сначала маленький slice, потом следующий. No big refactors unless explicitly requested.

Current important concepts
Parser boundary

The app currently depends on:

IInvoiceParser

It returns:

InvoiceParseResult

This contract should stay unchanged unless explicitly approved.

Current parser wired in DI:

LlmInvoiceParser : IInvoiceParser

Extractor mode is config-based:
- Demo mode: LlmInvoiceParser backed by DemoLlmInvoiceExtractor
- Real mode: LlmInvoiceParser backed by ClaudeInvoiceExtractor

FakeInvoiceParser is a local fake/test utility, not the current DI parser.

LLM types:

LlmInvoiceParser : IInvoiceParser
ILlmInvoiceExtractor
LlmExtractionResult
LlmExtractedFields

LLM output is a proposal, not final truth.

Claude/LLM must only extract invoice data. It must not decide supplier creation, bank risk, review approval, or Exact posting.

LLM extraction direction

Target future flow:

PDF/image invoice
→ text extraction / OCR
→ Claude/LLM extraction
→ strict JSON
→ LlmExtractedFields
→ LlmInvoiceParser
→ InvoiceParseResult
→ existing validation/matching/review/outbox flow

Standing rules:

Do not call real Claude API unless explicitly asked.
Do not add API keys to source code.
Do not modify Program.cs unless the task explicitly includes DI wiring.
Do not change IInvoiceParser signature.
Extraction metadata

Uploaded invoices may contain extraction metadata:

ExtractionModel
ExtractionCompletedAtUtc
RawExtractionJson
ExtractionWarnings
ExtractionError

These fields are used for audit/debugging and future LLM integration.

raw LLM output should be preserved, but business decisions happen later.

Review workflow

Manual review currently supports:

approve review
reject review
review decision
reviewed timestamp
review comment
review summary in responses

Approve means: continue to next automated step.

Reject means: keep invoice blocked / needs manual action.

Do not confuse extraction with review decision.

Supplier matching

Supplier matching and bank risk are separate responsibilities.

Supplier matching answers:

Who is this supplier?

Bank risk answers:

Is it safe to use these bank details?

Do not auto-post if bank details are new, changed, or conflicting.

Future supplier matching may use scored matching:

## Supplier profile notes
- Suppliers are international — KvK/VAT often empty
- G/L Account (grootboekrekening) is NOT extracted by LLM
- G/L Account comes from supplier profile or human review
- IBAN formats vary: NL, GB, IN and others

KvK exact match = auto match
VAT exact match = auto match
IBAN match = review
fuzzy name/city = review
no good match = create supplier candidate
Important interfaces/classes

Look at these before changing workflow:

IInvoiceParser
InvoiceParseResult
InvoiceUploadService
UploadedInvoiceRecord
UploadedInvoiceEntity
IUploadedInvoiceStore
EfUploadedInvoiceStore
ISupplierMatcher
SupplierMatchResult
IBankDetailsRiskEvaluator
BankDetailsRiskResult
IExactPostOutboxWriter
ISupplierCreateOutboxWriter
IInvoiceReviewService
InvoiceReviewService
IInvoiceRetryService
InvoiceRetryService
InvoicesController
Coding rules

Use C# with braces everywhere.

Correct:

if (condition)
{
    DoSomething();
}

Incorrect:

if (condition)
    DoSomething();

Do not remove braces.

Keep changes minimal.

Do not rename public contracts unless explicitly asked.

Do not introduce AutoMapper unless explicitly asked.

Do not introduce broad abstractions for small mapping code.

Do not change unrelated files.

Do not modify CI/CD YAML files unless explicitly asked.

Do not modify migrations unless the task explicitly changes persistence.

Testing rules

For every change, add or update focused tests.

Prefer existing test style.

Run focused tests first.

Then run full tests before final summary.

Useful commands:

dotnet test InvoiceFlow.Api.Tests\InvoiceFlow.Api.Tests.csproj

Focused example:

dotnet test InvoiceFlow.Api.Tests\InvoiceFlow.Api.Tests.csproj --filter "FullyQualifiedName~LlmInvoiceParserTests"
Safety rules for edits

Before editing, first explain:

What files you will read
What files you plan to change
Why the change is needed
What tests you will run

Do not edit until the plan is clear.

If a task says “analyze only”, do not edit files.

If a task says “new files only”, do not modify existing files.

If you need to modify more files than expected, stop and explain why.

Secrets and API keys

Never hardcode API keys.

Do not commit secrets.

Use:

user secrets locally
environment variables
Key Vault later

For Claude/Anthropic integration, API key must come from configuration, not source code.

Preferred PR style

Each PR should be small.

Good PR examples:

add LLM extraction models
add LLM parser adapter
add PDF text extractor
add Claude prompt builder
add supplier scoring logic
add one endpoint
add one persistence field + migration

Bad PR examples:

rewrite parser + supplier matching + Exact integration together
change IInvoiceParser and review flow in same PR
add real Claude API and OCR and Exact posting together
Current LLM implementation status

All extraction stages are complete and wired:

- IInvoiceTextExtractor / PdfPigTextExtractor — text extraction from PDFs
- ClaudePromptBuilder — prompt construction
- ClaudeInvoiceExtractor — real Claude API via HttpClient
- LlmInvoiceParser wired into DI in all modes; extractor selection is config-based:
  - Demo mode uses DemoLlmInvoiceExtractor
  - Real mode uses ClaudeInvoiceExtractor
- Opt-in integration tests: RUN_CLAUDE_INTEGRATION_TESTS=true + ANTHROPIC_API_KEY

PdfPig is for text-based PDFs. Scanned PDFs need OCR — not yet implemented.

Error handling guidance

Expected LLM failures should return structured extraction failure results.

Do not throw for normal LLM extraction failures such as:

timeout
malformed JSON
missing fields
low confidence
unsupported scanned PDF

Unexpected exceptions should be handled safely and tested.

Client-facing API messages should be generic where security matters.

Detailed errors should go to logs or internal metadata, not raw API responses.

Final response format after work

After completing a task, summarize:

files changed
behavior changed
tests run
result
risks
next recommended step

Keep the summary short and practical.