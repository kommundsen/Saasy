---
title: Update CONTEXT.md glossary for Commitment / Minimum / Credit
iteration: 11
status: todo
labels: [documentation]
depends-on: []
agent: human
---

# Update CONTEXT.md glossary

Add canonical definitions and relationships for `Commitment`, `Minimum`, `Credit`, `CreditBalance`, `CreditEntry`. All future code + docs must use this vocabulary verbatim per project convention.

## Acceptance criteria

- `CONTEXT.md` updated with new terms grouped under a "Billing adjustments" section.
- Cross-context relationships diagrammed (Commitment lives in Subscriptions, applied during Invoice generation in Invoicing context, draws from projection of Subscription/Customer).
- Existing terms unchanged.
