# agent-task.ps1
# Safe local status helper for InvoiceFlow agent workflow.
# Does NOT commit, push, merge, or call external tools.
#
# Usage:
#   .\scripts\agent-task.ps1
#   .\scripts\agent-task.ps1 -TaskId PR21

param(
    [string]$TaskId = ""
)

$repo   = Split-Path -Leaf (git rev-parse --show-toplevel 2>$null)
$branch = git rev-parse --abbrev-ref HEAD 2>$null
$commit = git rev-parse --short HEAD 2>$null
$dirty  = git status --short 2>$null

Write-Host ""
Write-Host "=== InvoiceFlow Agent Context ===" -ForegroundColor Cyan
Write-Host "Repo   : $repo"
Write-Host "Branch : $branch"
Write-Host "Commit : $commit"
Write-Host ""

if ($dirty) {
    Write-Host "Dirty files:" -ForegroundColor Yellow
    $dirty | ForEach-Object { Write-Host "  $_" }
} else {
    Write-Host "Working tree: clean" -ForegroundColor Green
}

Write-Host ""

if ($TaskId -ne "") {
    if (Test-Path "TASKS.md") {
        $lines  = Get-Content "TASKS.md" -Encoding UTF8
        $inTask = $false
        $found  = $false

        foreach ($line in $lines) {
            if ($line -match "###.*$TaskId") {
                $inTask = $true
                $found  = $true
            } elseif ($line -match "^###" -and $inTask) {
                break
            }

            if ($inTask) { Write-Host $line }
        }

        if (-not $found) {
            Write-Host "Task '$TaskId' not found in TASKS.md." -ForegroundColor Yellow
        }
    } else {
        Write-Host "TASKS.md not found." -ForegroundColor Yellow
    }

    Write-Host ""
}

Write-Host "Suggested commands:" -ForegroundColor Cyan
Write-Host "  dotnet test InvoiceFlow.Api.Tests\InvoiceFlow.Api.Tests.csproj"
Write-Host "  git diff --stat"
Write-Host "  git log --oneline -5"
Write-Host "  git diff main...HEAD"
Write-Host ""
