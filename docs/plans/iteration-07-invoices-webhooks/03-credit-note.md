---
title: CreditNote aggregate + correction flow
iteration: 07
status: todo
labels: [invoicing, domain]
depends-on: [00-invoice-lineitem]
---

# CreditNote aggregate

A `CreditNote` is an Aggregate Root referencing a target `InvoiceId`. Used for full or partial corrections after an Invoice is issued — Saasy never edits issued Invoices in place.

## Acceptance criteria

- `CreditNote.Issue(invoiceId, lineItems, reason)` produces a new aggregate with negative-amount LineItems referencing the original.
- An Invoice's `Status` updates to `CreditedFully` or `CreditedPartial` based on cumulative CreditNote amounts.
- Currency must match target Invoice.
- Domain Event `invoice.credit-note-issued` published.
- API: `POST /v1/invoices/{id}/credit-notes`.
