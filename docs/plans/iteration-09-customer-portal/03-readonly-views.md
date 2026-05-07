---
title: Read-only usage + invoices views
iteration: 09
status: todo
labels: [frontend, customer-portal]
depends-on: [00-portal-scaffold, 02-jwt-auth-placeholder]
---

# Read-only usage + invoices views

Pages: `/usage` (current Period rollups + threshold progress bars), `/invoices` (recent Invoices with download links to JSON/HTML), `/plan` (current Plan + PlanVersion summary).

## Acceptance criteria

- Endpoint surface (read-only):
  - `GET /v1/portal/me` — current customer + active subscription summary.
  - `GET /v1/portal/me/usage` — current rollups with quota progress.
  - `GET /v1/portal/me/invoices` — paginated invoice list.
- Charts use a small dependency (e.g. Recharts) — no full dashboarding lib.
- All endpoints scope to `customer_id` from token; cross-customer access returns 404.
