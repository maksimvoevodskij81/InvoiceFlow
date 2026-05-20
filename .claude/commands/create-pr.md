# Skill: Create PR for InvoiceFlow

## Steps

1. Run full test suite first
   dotnet test InvoiceFlow.Api.Tests\InvoiceFlow.Api.Tests.csproj
   If any test fails — STOP. Do not commit. Report the failure.

2. Show summary of changed files
   git status
   git diff --stat

3. Stage only relevant files
   git add <only the files changed for this task>
   Never use git add . without listing what will be staged first.

4. Commit with conventional message
   Format: type(scope): short description
   Types: feat | fix | refactor | test | chore
   Examples:
   - feat(extraction): add ExtractionFailed status
   - fix(parser): handle null LlmExtractedFields
   - refactor(upload): cast to IExtractionMetadataProvider

5. Push
   git push

6. Update PROGRESS.md
   Mark completed item as [x]
   Add next planned PR if known

7. Print summary
   Files changed:
   Tests: X passed, 0 failed
   Risks: (any noted)
   Next recommended step:

## Rules
- Never commit if tests fail
- Never use git add . blindly
- Never hardcode secrets
- Always update PROGRESS.md after push
