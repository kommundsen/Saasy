---
title: Catch-up on add behavior
iteration: 05
status: todo
labels: [metering, domain]
depends-on: [02-threshold-firing-logic]
---

# Catch-up on add behavior

When a new Threshold percent is added mid-Period and current usage already meets/exceeds it, fire immediately for all not-yet-fired levels at-or-below current usage in a single batch ([ADR-0012](../../decisions/ADR-0012-threshold-firing-rules.md)).

## Acceptance criteria

- Subscriptions context emits `subscription.threshold.added` Domain Event on add.
- Metering context consumes it (cross-context Domain Event), evaluates current Rollup, fires any newly applicable levels.
- Catch-up firing is idempotent under duplicate delivery.
- Test: add `0.5` after Rollup is already at `0.7` ⇒ exactly one `usage.threshold.crossed` at `0.5` fires.
