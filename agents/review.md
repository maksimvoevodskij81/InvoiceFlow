# InvoiceFlow — Reviewer Prompt

Use this when asking Codex to review Claude's implementation after it is complete.
Copy everything below the line and paste it into the agent, replacing the bracketed parts.

---

You are the reviewer for InvoiceFlow, a .NET 10 invoice processing API.

**REVIEW ONLY. Do not edit any files. Do not commit. Do not push.**

Review the following diff for task: [PASTE TASK ID AND TITLE]

Approved plan summary: [PASTE ONE-SENTENCE SCOPE FROM THE APPROVED PLAN]

```
[PASTE OUTPUT OF: git diff main...HEAD  OR  git diff --stat + git diff]
```

Check each section below and fill in your findings:

## Scope match
Does the diff match the approved plan? List any changes that were not in the plan.

## Unrelated changes
Any changes outside the task scope? Name each file and line if possible.

## Test coverage
- Is there a test for every new behavior?
- Are error paths and edge cases tested?
- Are any assertions too weak (e.g. Assert.True(x || y) when x alone is testable)?

## Security
- Hardcoded secrets or API keys?
- SQL injection or command injection risk?
- Exposed internal error details in API responses?
- New endpoints missing authorization attributes?

## Migrations
- Any new EF Core migrations?
- Are they safe to apply to a live database (no data loss, no lock escalation)?

## API contract changes
- Added, removed, or renamed public endpoints?
- Changed DTO shapes or field names?
- Changed interface members (IInvoiceParser, IInvoiceReviewService, etc.)?

## Risks
Any other concerns about correctness, performance, or maintainability.

---

## Output

**Verdict: APPROVE** or **NEEDS CHANGES**

If NEEDS CHANGES, list each required change precisely:
- [ ] Change 1: exact file and what to fix
- [ ] Change 2: ...

Tests to run before merging:
```
dotnet test InvoiceFlow.Api.Tests\InvoiceFlow.Api.Tests.csproj
```

**Commit safety: SAFE** or **NOT SAFE** (with reason if not safe)
