# Saasy Customer Portal UI Kit

The end-customer-facing usage and billing portal. Embed-friendly. Shows current Subscription, live Rollups vs Quotas, recent Invoices, and threshold notifications. End customers do **not** authenticate to Saasy directly — they auth into the Integrator's app, and the Integrator embeds this portal with a short-lived Saasy session token.

Open `index.html` to see the demo.

## Screens

1. **Usage overview** (default) — Plan summary, live quota meters, period timeline.
2. **Invoice detail** — line items grouped by Pricing Component, late-arrival adjustment row.

## Embed-aware chrome

The portal is a single-column, `max-width: 1080px` layout with no left sidebar — it sits inside an Integrator's product chrome. The `[data-portal]` body attribute uses `--portal-bg: var(--paper)` so a white-themed app frames cleanly; pass `data-portal-bg="muted"` to use the warm muted background when the surrounding page is white.
