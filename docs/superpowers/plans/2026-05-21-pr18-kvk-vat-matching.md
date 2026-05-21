# PR 18 — KvK/VAT Supplier Matching (review-first) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add KvK and VAT fingerprint-based supplier matching to `MappingBasedSupplierMatcher`, checked before IBAN and name, with `RequiresReview = true` for all matches (the safe "review-first" slice before PR 19 enables auto-match).

**Architecture:** KvK/VAT numbers are normalised into fingerprint strings (`KVK:{digits}` / `VAT:{normalised}`) and looked up in the existing `SupplierMappings` table (same generic fingerprint-to-ExactSupplierId table already used for name and bank account fingerprints). No DB migration needed — the fingerprint string is already the key. New constants are added to `SupplierMatchSources` so callers can tell how a match was found.

**Tech Stack:** C# / xUnit — same stack as every other test in the project. Run tests with `dotnet test InvoiceFlow.Api.Tests/InvoiceFlow.Api.Tests.csproj`.

---

## Context and existing state

These fields **already exist** — do not add them again:
- `InvoiceParseResult.SupplierKvKNumber` / `SupplierVatNumber`
- `LlmExtractedFields.SupplierKvKNumber` / `SupplierVatNumber`
- `LlmInvoiceParser` maps them directly (no change needed)
- `SupplierMappingEntity` uses a generic `Fingerprint` string — no schema change needed

Matching priority after PR 18 (all `RequiresReview = true`):
1. KvK fingerprint — skip if empty
2. VAT fingerprint — skip if empty
3. IBAN / bank account fingerprint — skip if empty
4. Name + postcode
5. Name + address + postcode
6. No match

PR 19 will change KvK/VAT to `RequiresReview = false`.

---

## File map

| Action | File |
|---|---|
| Modify | `InvoiceFlow.Api/Features/Invoices/SupplierMatchSources.cs` |
| Modify | `InvoiceFlow.Api/Features/Suppliers/Idempotency/SupplierFingerPrintBuilder.cs` |
| Modify | `InvoiceFlow.Api/Features/Suppliers/Matching/MappingBasedSupplierMatcher.cs` |
| Create | `InvoiceFlow.Api.Tests/Features/Suppliers/Idempotency/SupplierFingerprintBuilderKvkVatTests.cs` |
| Modify | `InvoiceFlow.Api.Tests/Features/Suppliers/Matching/MappingBasedSupplierMatcherTests.cs` |

---

## Task 1: KvK/VAT fingerprint methods + SupplierMatchSources constants

**Files:**
- Modify: `InvoiceFlow.Api/Features/Invoices/SupplierMatchSources.cs`
- Modify: `InvoiceFlow.Api/Features/Suppliers/Idempotency/SupplierFingerPrintBuilder.cs`
- Create: `InvoiceFlow.Api.Tests/Features/Suppliers/Idempotency/SupplierFingerprintBuilderKvkVatTests.cs`

- [ ] **Step 1: Write failing tests**

Create `InvoiceFlow.Api.Tests/Features/Suppliers/Idempotency/SupplierFingerprintBuilderKvkVatTests.cs`:

```csharp
using InvoiceFlow.Api.Features.Suppliers.Idempotency;

namespace InvoiceFlow.Api.Tests.Features.Suppliers.Idempotency;

public sealed class SupplierFingerprintBuilderKvkVatTests
{
    private readonly SupplierFingerprintBuilder _builder = new();

    [Fact]
    public void BuildKvK_ShouldReturnKvkFingerprint_WhenPlainNumber()
    {
        Assert.Equal("KVK:12345678", _builder.BuildKvK("12345678"));
    }

    [Fact]
    public void BuildKvK_ShouldStripSpacesAndNonDigits_WhenFormatted()
    {
        Assert.Equal("KVK:12345678", _builder.BuildKvK("12 34 56 78"));
    }

    [Fact]
    public void BuildKvK_ShouldReturnEmpty_WhenNull()
    {
        Assert.Equal(string.Empty, _builder.BuildKvK(null));
    }

    [Fact]
    public void BuildKvK_ShouldReturnEmpty_WhenWhitespace()
    {
        Assert.Equal(string.Empty, _builder.BuildKvK("   "));
    }

    [Fact]
    public void BuildKvK_ShouldReturnEmpty_WhenNoDigitsPresent()
    {
        Assert.Equal(string.Empty, _builder.BuildKvK("no-digits-here"));
    }

    [Fact]
    public void BuildVat_ShouldReturnVatFingerprint_WhenDutchVat()
    {
        Assert.Equal("VAT:NL123456789B01", _builder.BuildVat("NL123456789B01"));
    }

    [Fact]
    public void BuildVat_ShouldStripSpaces_WhenVatHasSpaces()
    {
        Assert.Equal("VAT:NL123456789B01", _builder.BuildVat("NL 123456789 B01"));
    }

    [Fact]
    public void BuildVat_ShouldUppercase_WhenLowercase()
    {
        Assert.Equal("VAT:GB123456789", _builder.BuildVat("gb123456789"));
    }

    [Fact]
    public void BuildVat_ShouldReturnEmpty_WhenNull()
    {
        Assert.Equal(string.Empty, _builder.BuildVat(null));
    }

    [Fact]
    public void BuildVat_ShouldReturnEmpty_WhenWhitespace()
    {
        Assert.Equal(string.Empty, _builder.BuildVat("   "));
    }
}
```

- [ ] **Step 2: Run to verify RED**

```
dotnet test "InvoiceFlow.Api.Tests/InvoiceFlow.Api.Tests.csproj" --filter "FullyQualifiedName~SupplierFingerprintBuilderKvkVatTests"
```

Expected: build error — `BuildKvK` and `BuildVat` do not exist yet.

- [ ] **Step 3: Add constants to SupplierMatchSources**

Edit `InvoiceFlow.Api/Features/Invoices/SupplierMatchSources.cs` — add two constants:

```csharp
namespace InvoiceFlow.Api.Features.Invoices;

public static class SupplierMatchSources
{
    public const string BankAccount = "BankAccount";
    public const string Name = "Name";
    public const string CreatedInExact = "CreatedInExact";
    public const string KvK = "KvK";
    public const string Vat = "Vat";
}
```

- [ ] **Step 4: Add BuildKvK and BuildVat to SupplierFingerprintBuilder**

Edit `InvoiceFlow.Api/Features/Suppliers/Idempotency/SupplierFingerPrintBuilder.cs` — add the two methods below the existing ones (before the private `NormalizeText` helper):

```csharp
public string BuildKvK(string? kvkNumber)
{
    if (string.IsNullOrWhiteSpace(kvkNumber))
    {
        return string.Empty;
    }

    string digits = new string(kvkNumber.Where(char.IsDigit).ToArray());

    if (string.IsNullOrEmpty(digits))
    {
        return string.Empty;
    }

    return $"KVK:{digits}";
}

public string BuildVat(string? vatNumber)
{
    if (string.IsNullOrWhiteSpace(vatNumber))
    {
        return string.Empty;
    }

    string normalized = System.Text.RegularExpressions.Regex
        .Replace(vatNumber, @"\s+", string.Empty)
        .ToUpperInvariant();

    if (string.IsNullOrEmpty(normalized))
    {
        return string.Empty;
    }

    return $"VAT:{normalized}";
}
```

You will also need to add `using System.Linq;` at the top if it is not already present (in .NET 10 it may be available via global usings — check after the build).

- [ ] **Step 5: Run to verify GREEN**

```
dotnet test "InvoiceFlow.Api.Tests/InvoiceFlow.Api.Tests.csproj" --filter "FullyQualifiedName~SupplierFingerprintBuilderKvkVatTests"
```

Expected: 10 tests pass.

- [ ] **Step 6: Run full suite to check no regressions**

```
dotnet test "InvoiceFlow.Api.Tests/InvoiceFlow.Api.Tests.csproj"
```

Expected: all tests pass (the new constants have no effect on existing tests).

- [ ] **Step 7: Commit**

```bash
git add "InvoiceFlow.Api/Features/Invoices/SupplierMatchSources.cs" \
        "InvoiceFlow.Api/Features/Suppliers/Idempotency/SupplierFingerPrintBuilder.cs" \
        "InvoiceFlow.Api.Tests/Features/Suppliers/Idempotency/SupplierFingerprintBuilderKvkVatTests.cs"
git commit -m "feat(matching): add KvK/VAT fingerprint methods and SupplierMatchSources constants"
```

---

## Task 2: KvK/VAT matching in MappingBasedSupplierMatcher

**Files:**
- Modify: `InvoiceFlow.Api/Features/Suppliers/Matching/MappingBasedSupplierMatcher.cs`
- Modify: `InvoiceFlow.Api.Tests/Features/Suppliers/Matching/MappingBasedSupplierMatcherTests.cs`

The existing test file uses `file`-scoped `FakeSupplierMappingStore` and `FakeBankAccountMappingStore` — add the new tests to the same class and reuse those fakes.

- [ ] **Step 1: Write failing tests**

Add the following tests to `MappingBasedSupplierMatcherTests` (append before the closing `}` of the class, not after the file-scoped fakes):

```csharp
[Fact]
public async Task MatchAsync_ShouldReturnMatchedWithReview_WhenKvkMappingFound()
{
    string fingerprint = new SupplierFingerprintBuilder().BuildKvK("12345678");
    var supplierStore = new FakeSupplierMappingStore();
    supplierStore.Seed(fingerprint, "exact-kvk-01");

    var result = await BuildMatcher(supplierStore: supplierStore).MatchAsync(
        new InvoiceParseResult
        {
            SupplierName    = "Acme B.V.",
            SupplierKvKNumber = "12345678"
        },
        CancellationToken.None);

    Assert.True(result.IsMatched);
    Assert.True(result.RequiresReview);
    Assert.Equal("exact-kvk-01", result.ExactSupplierId);
    Assert.Equal(SupplierMatchSources.KvK, result.MatchedBy);
}

[Fact]
public async Task MatchAsync_ShouldReturnMatchedWithReview_WhenVatMappingFound()
{
    string fingerprint = new SupplierFingerprintBuilder().BuildVat("NL123456789B01");
    var supplierStore = new FakeSupplierMappingStore();
    supplierStore.Seed(fingerprint, "exact-vat-01");

    var result = await BuildMatcher(supplierStore: supplierStore).MatchAsync(
        new InvoiceParseResult
        {
            SupplierName      = "Acme B.V.",
            SupplierVatNumber = "NL123456789B01"
        },
        CancellationToken.None);

    Assert.True(result.IsMatched);
    Assert.True(result.RequiresReview);
    Assert.Equal("exact-vat-01", result.ExactSupplierId);
    Assert.Equal(SupplierMatchSources.Vat, result.MatchedBy);
}

[Fact]
public async Task MatchAsync_ShouldPreferKvk_WhenBothKvkAndVatAreMapped()
{
    var supplierStore = new FakeSupplierMappingStore();
    supplierStore.Seed(new SupplierFingerprintBuilder().BuildKvK("12345678"), "exact-kvk-win");
    supplierStore.Seed(new SupplierFingerprintBuilder().BuildVat("NL123456789B01"), "exact-vat-lose");

    var result = await BuildMatcher(supplierStore: supplierStore).MatchAsync(
        new InvoiceParseResult
        {
            SupplierName      = "Acme B.V.",
            SupplierKvKNumber = "12345678",
            SupplierVatNumber = "NL123456789B01"
        },
        CancellationToken.None);

    Assert.Equal("exact-kvk-win", result.ExactSupplierId);
    Assert.Equal(SupplierMatchSources.KvK, result.MatchedBy);
}

[Fact]
public async Task MatchAsync_ShouldPreferKvk_WhenBothKvkAndIbanAreMapped()
{
    var supplierStore = new FakeSupplierMappingStore();
    supplierStore.Seed(new SupplierFingerprintBuilder().BuildKvK("12345678"), "exact-kvk-win");

    var bankStore = new FakeBankAccountMappingStore();
    bankStore.Seed("IBAN:NL91ABNA0417164300", "exact-iban-lose");

    var result = await BuildMatcher(supplierStore: supplierStore, bankStore: bankStore).MatchAsync(
        new InvoiceParseResult
        {
            SupplierName      = "Acme B.V.",
            SupplierKvKNumber = "12345678",
            SupplierBankAccount = "NL91 ABNA 0417 1643 00"
        },
        CancellationToken.None);

    Assert.Equal("exact-kvk-win", result.ExactSupplierId);
    Assert.Equal(SupplierMatchSources.KvK, result.MatchedBy);
}

[Fact]
public async Task MatchAsync_ShouldPreferVat_WhenVatAndIbanAreMappedButKvkIsAbsent()
{
    var supplierStore = new FakeSupplierMappingStore();
    supplierStore.Seed(new SupplierFingerprintBuilder().BuildVat("NL123456789B01"), "exact-vat-win");

    var bankStore = new FakeBankAccountMappingStore();
    bankStore.Seed("IBAN:NL91ABNA0417164300", "exact-iban-lose");

    var result = await BuildMatcher(supplierStore: supplierStore, bankStore: bankStore).MatchAsync(
        new InvoiceParseResult
        {
            SupplierName        = "Acme B.V.",
            SupplierVatNumber   = "NL123456789B01",
            SupplierBankAccount = "NL91 ABNA 0417 1643 00"
        },
        CancellationToken.None);

    Assert.Equal("exact-vat-win", result.ExactSupplierId);
    Assert.Equal(SupplierMatchSources.Vat, result.MatchedBy);
}

[Fact]
public async Task MatchAsync_ShouldSkipKvkCheck_WhenKvkNumberIsNull()
{
    // Even if a KVK fingerprint happened to be stored, a null KvK on the
    // invoice must not produce a false match.
    var supplierStore = new FakeSupplierMappingStore();
    supplierStore.Seed("KVK:", "should-never-be-returned");

    var result = await BuildMatcher(supplierStore: supplierStore).MatchAsync(
        new InvoiceParseResult { SupplierName = "Acme B.V.", SupplierKvKNumber = null },
        CancellationToken.None);

    Assert.False(result.IsMatched);
}

[Fact]
public async Task MatchAsync_ShouldSkipVatCheck_WhenVatNumberIsNull()
{
    var supplierStore = new FakeSupplierMappingStore();
    supplierStore.Seed("VAT:", "should-never-be-returned");

    var result = await BuildMatcher(supplierStore: supplierStore).MatchAsync(
        new InvoiceParseResult { SupplierName = "Acme B.V.", SupplierVatNumber = null },
        CancellationToken.None);

    Assert.False(result.IsMatched);
}
```

- [ ] **Step 2: Run to verify RED**

```
dotnet test "InvoiceFlow.Api.Tests/InvoiceFlow.Api.Tests.csproj" --filter "FullyQualifiedName~MappingBasedSupplierMatcherTests"
```

Expected: compile error — `SupplierMatchSources.KvK` and `SupplierMatchSources.Vat` exist (added in Task 1), but the matcher does not yet use KvK/VAT logic so the match tests will fail with `IsMatched = false` instead of returning a KvK/VAT match.

- [ ] **Step 3: Implement KvK/VAT matching in MappingBasedSupplierMatcher**

Replace the body of `MatchAsync` in `InvoiceFlow.Api/Features/Suppliers/Matching/MappingBasedSupplierMatcher.cs`:

```csharp
public async Task<SupplierMatchResult> MatchAsync(
    InvoiceParseResult parseResult,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(parseResult);

    string kvkFingerprint = _supplierFingerprintBuilder.BuildKvK(parseResult.SupplierKvKNumber);
    if (!string.IsNullOrEmpty(kvkFingerprint))
    {
        string? kvkId = await _supplierMappingStore.FindExactSupplierIdAsync(kvkFingerprint, cancellationToken);
        if (!string.IsNullOrWhiteSpace(kvkId))
        {
            return Matched(kvkId, SupplierMatchSources.KvK, "Matched by KvK number.");
        }
    }

    string vatFingerprint = _supplierFingerprintBuilder.BuildVat(parseResult.SupplierVatNumber);
    if (!string.IsNullOrEmpty(vatFingerprint))
    {
        string? vatId = await _supplierMappingStore.FindExactSupplierIdAsync(vatFingerprint, cancellationToken);
        if (!string.IsNullOrWhiteSpace(vatId))
        {
            return Matched(vatId, SupplierMatchSources.Vat, "Matched by VAT number.");
        }
    }

    string bankFingerprint = _bankAccountFingerprintBuilder.Build(parseResult.SupplierBankAccount);
    if (!string.IsNullOrEmpty(bankFingerprint))
    {
        string? exactId = await _bankAccountMappingStore.FindExactSupplierIdAsync(bankFingerprint, cancellationToken);
        if (!string.IsNullOrWhiteSpace(exactId))
        {
            return Matched(exactId, SupplierMatchSources.BankAccount, "Matched by IBAN fingerprint.");
        }
    }

    string namePostcodeFingerprint = _supplierFingerprintBuilder.BuildNamePostcode(parseResult);
    string? namePostcodeId = await _supplierMappingStore.FindExactSupplierIdAsync(namePostcodeFingerprint, cancellationToken);
    if (!string.IsNullOrWhiteSpace(namePostcodeId))
    {
        return Matched(namePostcodeId, SupplierMatchSources.Name, "Matched by name and postcode fingerprint.");
    }

    string nameAddrPostcodeFingerprint = _supplierFingerprintBuilder.BuildNameAddressPostcode(parseResult);
    string? nameAddrPostcodeId = await _supplierMappingStore.FindExactSupplierIdAsync(nameAddrPostcodeFingerprint, cancellationToken);
    if (!string.IsNullOrWhiteSpace(nameAddrPostcodeId))
    {
        return Matched(nameAddrPostcodeId, SupplierMatchSources.Name, "Matched by name, address, and postcode fingerprint.");
    }

    return new SupplierMatchResult { IsMatched = false };
}
```

Also add the using at the top if not already present:
```csharp
using InvoiceFlow.Api.Features.Invoices;
```
(It already imports `InvoiceFlow.Api.Features.Invoices` via `InvoiceFlow.Api.Features.Invoices.ImportInvoicesFromFolder` — but `SupplierMatchSources` is in `InvoiceFlow.Api.Features.Invoices` namespace, so verify the using covers it.)

- [ ] **Step 4: Run focused tests to verify GREEN**

```
dotnet test "InvoiceFlow.Api.Tests/InvoiceFlow.Api.Tests.csproj" --filter "FullyQualifiedName~MappingBasedSupplierMatcherTests"
```

Expected: all 13 tests pass (6 existing + 7 new).

- [ ] **Step 5: Run full suite**

```
dotnet test "InvoiceFlow.Api.Tests/InvoiceFlow.Api.Tests.csproj"
```

Expected: all tests pass, 0 failed.

- [ ] **Step 6: Commit**

```bash
git add "InvoiceFlow.Api/Features/Suppliers/Matching/MappingBasedSupplierMatcher.cs" \
        "InvoiceFlow.Api.Tests/Features/Suppliers/Matching/MappingBasedSupplierMatcherTests.cs"
git commit -m "feat(matching): add KvK/VAT matching to MappingBasedSupplierMatcher (review-first)"
```

---

## Task 3: Update PROGRESS.md and push

- [ ] **Step 1: Mark PR 18 done in PROGRESS.md**

In `PROGRESS.md`, change:
```
- [ ] PR 18 — Supplier KvK/VAT matching, review-first ...
```
to:
```
- [x] PR 18 — Supplier KvK/VAT matching, review-first ...
```

- [ ] **Step 2: Commit and push**

```bash
git add PROGRESS.md
git commit -m "chore: update PROGRESS.md — mark PR 18 done"
git push
```

---

## Verification summary

```
dotnet test "InvoiceFlow.Api.Tests/InvoiceFlow.Api.Tests.csproj" --filter "FullyQualifiedName~SupplierFingerprintBuilderKvkVatTests"
# → 10 tests pass

dotnet test "InvoiceFlow.Api.Tests/InvoiceFlow.Api.Tests.csproj" --filter "FullyQualifiedName~MappingBasedSupplierMatcherTests"
# → 13 tests pass (6 pre-existing + 7 new)

dotnet test "InvoiceFlow.Api.Tests/InvoiceFlow.Api.Tests.csproj"
# → all tests pass, 0 failed
```
