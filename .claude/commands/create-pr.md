Create file .claude/commands/create-pr.md with this content:

# Create PR

## Step 1 — Simplify first
Run /simplify on all changed files.
Review suggestions, apply only what makes sense.
Skip suggestions that would change behavior.

## Step 2 — Run full tests
dotnet test InvoiceFlow.Api.Tests\InvoiceFlow.Api.Tests.csproj

If any test fails — STOP. Fix first. Do not proceed.

## Step 3 — Show changed files
git status
git diff --stat

## Step 4 — Commit
Stage only relevant files.
Never git add . without showing file list first.
Commit with conventional message (feat/fix/refactor/test/chore).

## Step 5 — Push
git push

## Step 6 — Update PROGRESS.md
Mark current task as [x].
Add next planned PR.

## Step 7 — Summary
Files changed:
Tests: X passed
Simplifications applied: yes/no
Risks if any:
Next recommended step: