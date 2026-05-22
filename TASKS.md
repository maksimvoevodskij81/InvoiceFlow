# InvoiceFlow — Task Queue

Active tasks only. One task per PR. One branch per task.

For completed PRs and architectural decisions, see PROGRESS.md.

**Parallel work rules:**
- Only run tasks in parallel when their files and scope do not overlap.
- Use a separate branch and worktree per parallel task.
- If overlap is discovered, stop and ask the user which task continues.
- Sequential work is required for: Program.cs, InvoiceUploadService, InvoiceRetryService,
  InvoicesController, EF migrations, shared DTOs.

---

## Task statuses

| Status | Meaning |
|---|---|
| Backlog | Identified, not yet planned |
| Planning | Codex is preparing the plan |
| Ready | Plan approved, waiting for implementation |
| In Progress | Claude is implementing |
| Review | Codex is reviewing the diff |
| Done | Merged |
| Deferred | Postponed with reason |

---

## Task template

```
### [PR-XX] Title

| Field | Value |
|---|---|
| Status | Backlog |
| Branch | pr-XX-short-name |
| Owner | Claude |
| Reviewer | Codex |
| Scope | One sentence describing what this PR does |
| Out of scope | What this PR explicitly does NOT include |
| Files likely touched | List of files |
| Test command | dotnet test InvoiceFlow.Api.Tests\InvoiceFlow.Api.Tests.csproj --filter "..." |
| Acceptance criteria | - bullet list of observable outcomes |
```

---

## Tasks

### [PR-21] G/L Account Suggestion

| Field | Value |
|---|---|
| Status | Deferred |
| Branch | pr-21-gl-account |
| Owner | Claude |
| Reviewer | Codex |
| Scope | Suggest G/L account during invoice review, sourced from Exact supplier profile |
| Out of scope | Auto-posting, Exact API integration, LLM G/L extraction, new migrations |
| Files likely touched | TBD — depends on Exact supplier profile queryability |
| Test command | dotnet test InvoiceFlow.Api.Tests\InvoiceFlow.Api.Tests.csproj --filter "FullyQualifiedName~GLAccount" |
| Acceptance criteria | - Deferred until Exact supplier profiles are queryable from InvoiceFlow |

**Deferred reason:** G/L Account (grootboekrekening) cannot be extracted by LLM and must come
from the supplier profile in Exact Online. Exact supplier profiles are not yet queryable.
No automation planned until that integration exists.
