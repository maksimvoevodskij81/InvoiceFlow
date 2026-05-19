# InvoiceFlow — Progress

## Done
- [x] PR 1 — LLM extraction foundation (models, ILlmInvoiceExtractor, LlmInvoiceParser, PdfPigTextExtractor, DI wiring)
- [x] PR 2 — ClaudePromptBuilder (Build(text) → ClaudePrompt with system prompt + user message)
- [x] PR 3 — ClaudeInvoiceExtractor with FakeHttpMessageHandler tests (HTTP error, malformed JSON, cancellation)
- [x] PR 4 — Config-based DI switch (Demo vs Real mode, startup validation via ClaudeOptionsValidator)

## Planned
- [ ] PR 5 — Remove as LlmInvoiceParser cast (optional cleanup)

## Not started yet
- [ ] Supplier scored matching (KvK / VAT / IBAN / fuzzy name)
- [ ] ExtractionFailed as first-class invoice status
- [ ] Human correction flow (AcceptedInvoiceFields)
- [ ] Real Claude API integration test with PDF
