# InvoiceFlow — Progress

## Done
- [x] PR 1 — LLM extraction foundation (models, ILlmInvoiceExtractor, LlmInvoiceParser, PdfPigTextExtractor, DI wiring)

## In Progress
- [ ] PR 2 — ClaudePromptBuilder

## Planned
- [ ] PR 3 — ClaudeInvoiceExtractor with FakeHttpMessageHandler tests
- [ ] PR 4 — Config-based DI switch (Demo vs Real mode)
- [ ] PR 5 — Remove as LlmInvoiceParser cast (optional cleanup)

## Not started yet
- [ ] Supplier scored matching (KvK / VAT / IBAN / fuzzy name)
- [ ] ExtractionFailed as first-class invoice status
- [ ] Human correction flow (AcceptedInvoiceFields)
- [ ] Real Claude API integration test with PDF
