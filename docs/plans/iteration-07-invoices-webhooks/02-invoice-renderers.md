---
title: JSON + HTML renderers (deterministic)
iteration: 07
status: todo
labels: [invoicing, rendering]
depends-on: [00-invoice-lineitem]
agent: backend
---

# JSON + HTML renderers

Two renderers convert an Invoice aggregate to bytes. Both must be reproducible byte-for-byte from the same Invoice state (no timestamps in output, fixed key/element ordering, fixed locale).

## Acceptance criteria

- `JsonInvoiceRenderer` outputs canonicalized JSON (sorted keys, no whitespace variance).
- `HtmlInvoiceRenderer` outputs a self-contained HTML document; CSS inlined or linked to a versioned static asset.
- Both renderers exposed via `GET /v1/invoices/{id}.json` and `.html`.
- Property test: same input ⇒ same byte-for-byte output across N runs.
- No PDF in scope.
