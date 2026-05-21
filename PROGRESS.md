# InvoiceFlow — Progress

## Done
- [x] PR 1 — LLM extraction foundation (models, ILlmInvoiceExtractor, LlmInvoiceParser, PdfPigTextExtractor, DI wiring)
- [x] PR 2 — ClaudePromptBuilder (Build(text) → ClaudePrompt with system prompt + user message)
- [x] PR 3 — ClaudeInvoiceExtractor with FakeHttpMessageHandler tests (HTTP error, malformed JSON, cancellation)
- [x] PR 4 — Config-based DI switch (Demo vs Real mode, startup validation via ClaudeOptionsValidator)

- [x] PR 5 — Replace as LlmInvoiceParser cast with IExtractionMetadataProvider (middle-ground: interface cast, 3 files)
- [x] PR 6 — ExtractionFailed as first-class status (early return in InvoiceUploadService before validation, 2 focused tests)

- [x] PR 7 — MappingBasedSupplierMatcher (IBAN + name+postcode + name+addr+postcode fingerprints; wired into DI)

- [x] PR 8 — JWT Bearer auth + policy-based authorization (Authentication:Enabled flag, 4 policies, Swagger Bearer, 9 AuthorizationTests)

- [x] PR 9 — Robust bank account fingerprinting (IBAN: vs BANKACCOUNT: prefix, international supplier support, 8+1 tests)
- [x] PR 10 — Strengthen ClaudePromptBuilder for international invoices (ISO 4217 currency, exact bank account copy, KvK/VAT Dutch-only, no-guess rule, G/L prohibition, 12 tests)
- [x] PR 11 — Harden ClaudeInvoiceExtractor (IsRetryable on all paths, MissingApiKey guard, NetworkError catch, EnvelopeParseError, IsRetryable theory test, 5 new tests)
- [x] PR 12 — Real Claude API smoke test (opt-in via RUN_CLAUDE_INTEGRATION_TESTS + ANTHROPIC_API_KEY; skipped by default; ANTHROPIC_MODEL override; no new packages; no CI wiring)
- [x] PR 13 — Human correction flow: AcceptedInvoiceFields (5 core posting fields; Accepted* audit columns + overwrite main columns; ApproveReviewRequest DTO; EF migration; 4 focused tests)
- [x] PR 14 — Expose AcceptedFields in GetInvoiceDetailsResponse (AcceptedInvoiceFieldsResponse DTO; null when no corrections; helper method; 2 focused tests)
- [x] PR 15 — Expose ExtractedFields in GetInvoiceDetailsResponse (ExtractedInvoiceFieldsResponse DTO; deserialized from RawExtractionJson; null-safe + malformed-JSON-safe; no migration; 4 focused tests)
- [x] PR 16 — UploadedBy / ReviewedBy audit fields (JWT sub claim → nullable columns + migration; IInvoiceReviewService + IInvoiceUploadService signature change; GetCallerIdentity() in controller; 8 focused tests)

## Not started yet
- [ ] PR 17 — Manual retry for ExtractionFailed invoices (POST /invoices/{id}/retry-extraction; new IInvoiceRetryService)
- [ ] PR 18 — Supplier KvK/VAT matching, review-first (add KvK/VAT to InvoiceParseResult + SupplierMappingEntity; RequiresReview = true always)
- [ ] PR 19 — Supplier KvK/VAT auto-match with bank-risk guard (depends on PR 18; remove RequiresReview on safe KvK/VAT match)
- [ ] Real Claude API integration test with PDF
- [ ] G/L Account suggestion (future — stays manual input during review for now)

## Supplier matching context (from real Exact Online data)

Suppliers are international (NL, UK, India, and others). KvK and VAT are often
empty for non-Dutch suppliers. IBAN formats vary: NL IBAN, GB IBAN, Indian
account numbers. Matching must not assume any single field is always present.

Revised matching priority:
1. KvK fingerprint  → auto match, RequiresReview = false  (NL suppliers only — skip if empty)
2. VAT fingerprint  → auto match, RequiresReview = false  (NL suppliers only — skip if empty)
3. IBAN fingerprint → requires review, RequiresReview = true  (all suppliers, most common signal)
4. Fuzzy name + country → requires review  (fallback when no IBAN)
5. No match → create new supplier candidate

G/L Account (grootboekrekening) cannot be extracted by LLM. It must come from
the supplier profile in Exact or from human input during review. No automation
planned until supplier profiles are queryable.
