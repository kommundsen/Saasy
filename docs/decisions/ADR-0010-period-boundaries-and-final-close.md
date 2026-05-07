---
id: ADR-0010
type: adr
state: accepted
title: Period boundaries and Final Close
date: 2026-05-07
deciders: [kim]
---

# ADR-0010 — Period boundaries and Final Close

## Context

Two related questions about Billing Period semantics:

1. When does an Invoice issue, given that Late Events recompute Rollups inside a configurable Late Event Window? Issuing at Period Close conflicts with PRD §5's "100% reproducible Invoice" guarantee — a Late Event arriving after issuance recomputes the Rollup but cannot edit the frozen Invoice.
2. In which timezone do Period boundaries resolve, and what happens with `anchor-day` Cycle Anchors when the anchor day exceeds the target month's day count (Jan 31 → Feb ?)?

## Decision

### Period Close vs Final Close

- **Period Close** is the calendar end of the Billing Period. The Rollup is in its post-period state but is NOT frozen — Late Events arriving in the Window still recompute it.
- **Final Close** = Period Close + Late Event Window. At Final Close: the Rollup is frozen permanently, the InvoiceGenerator computes the Invoice from the frozen Rollup and Plan Version, the Invoice is Issued, and `InvoiceGenerated` raises.
- **Late Events** are accepted only between Period Close and Final Close. Past Final Close they are hard-rejected with HTTP 422 and error code `event.outside_late_event_window`. The error includes the Event's timestamp, Final Close, and Window length.

**Late Event Window:** default 24 hours; per-Integrator override allowed up to 7 days; lower bound is 1 hour (zero is not permitted). No per-Subscription override in v1.

**No Supplemental Invoices.** One document per period, issued once at Final Close. Credit Notes remain the manual correction path.

### Timezone

Each Integrator has a `Timezone` Value Object holding an IANA name (`Europe/Oslo`, `Asia/Tokyo`, …). Required at Integrator creation (no default). Forward-looking-mutable: changes apply to Periods computed after the change; already-issued Invoices and already-Final-Closed Periods stay in the Timezone they were resolved with.

**All Period boundaries resolve at 00:00 wall-clock in the Integrator's Timezone, then convert to UTC for storage.**

- `calendar-month` Cycle Anchor: 00:00 on the 1st of the month (or Jan/Apr/Jul/Oct for quarterly; Jan 1 for annual).
- `anchor-day` Cycle Anchor: 00:00 on the resolved anchor day (after clamping, see below).

DST is handled by `TimeZoneInfo`. Period boundaries at 00:00 dodge the problematic 02:00–03:00 wall-clock windows.

### Anchor-day clamping

When the Subscription's anchor day exceeds the target month's day count, clamp to the last day of that month; the original anchor day is preserved across the year and restored in months that have it. Industry-standard convention (Stripe, Chargebee, Recurly).

- Anchor 31, monthly: Jan 31 → Feb 28 (29 in leap years) → Mar 31 → Apr 30 → May 31 …
- Anchor Feb 29, annual: Feb 29 in leap years; Feb 28 otherwise.

The Subscription stores the original anchor day (1–31) as one integer. Per period: `day = Math.Min(anchorDay, DateTime.DaysInMonth(year, month))`.

For `calendar-month` Cycle Anchor, the anchor day field is stored as `1`.

## Consequences

- **"100% reproducible" Invoice holds at the single-document level.** Given the Events Saasy received within Period + Window, the Invoice is deterministic. No reconciliation across multiple documents.
- **Invoice latency = Late Event Window.** A January Invoice with a 24h Window issues 1 day late. Acceptable: Integrators' downstream billing pipelines run on multi-day cadences anyway.
- **Hard rejects past Final Close are visible to Integrators.** A 422 with a specific error code; no silent dead-letter. Integrators must size the Window to absorb their realistic ingestion latency.
- **Tokyo Customer of an SF Integrator sees Pacific-aligned periods.** Explicit trade-off: one canonical Timezone per Integrator vs per-Customer fan-out complexity. Customer Portal UI may translate to viewer-local for display.
- **Forward-looking-mutable Timezone is defensible.** Integrators can fix wrong selections without rewriting closed periods.
- **InvoiceGenerator does not run on a wall-clock cron.** Each Subscription's next Final Close is a deterministic UTC timestamp; the worker queries `WHERE next_final_close <= NOW()`.

### Out of scope

Per-Subscription Late Event Window or Timezone override; per-Customer Timezone display in the Portal (UI concern); backdated Subscription creation; an explicit `end-of-month` Cycle Anchor (anchor day 31 already covers this).
