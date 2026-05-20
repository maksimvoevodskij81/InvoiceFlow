# InvoiceFlow — Progress

## Done
- [x] PR 1 — LLM extraction foundation (models, ILlmInvoiceExtractor, LlmInvoiceParser, PdfPigTextExtractor, DI wiring)
- [x] PR 2 — ClaudePromptBuilder (Build(text) → ClaudePrompt with system prompt + user message)
- [x] PR 3 — ClaudeInvoiceExtractor with FakeHttpMessageHandler tests (HTTP error, malformed JSON, cancellation)
- [x] PR 4 — Config-based DI switch (Demo vs Real mode, startup validation via ClaudeOptionsValidator)

- [x] PR 5 — Replace as LlmInvoiceParser cast with IExtractionMetadataProvider (middle-ground: interface cast, 3 files)
- [x] PR 6 — ExtractionFailed as first-class status (early return in InvoiceUploadService before validation, 2 focused tests)

- [x] PR 7 — MappingBasedSupplierMatcher (IBAN + name+postcode + name+addr+postcode fingerprints; wired into DI)

## Not started yet
- [ ] Supplier scored matching (KvK / VAT / IBAN / fuzzy name)
- [ ] Human correction flow (AcceptedInvoiceFields)
- [ ] Real Claude API integration test with PDF
