# Saasy Ops Console UI Kit

Internal Saasy operator tools. NOT customer-facing — these are the dashboards Saasy on-call uses to monitor platform health, debug Integrators, and execute the runbooks in our ADRs.

Open `index.html` to see the demo.

## Why this exists separately

Per ADR-0014, our internal Ops Console is held to different standards than customer-facing surfaces — denser, more terminal-like, no marketing polish. It surfaces things end-users should never see: Late Event volumes, Final Close timing, Webhook DLQ, idempotency-key collision rate, per-Integrator quota saturation.

## Screens

1. **Platform health** (default) — global metrics, recent incidents, Late Event timeline.
2. **Integrator drill-in** — debug a single Integrator's flows, with Event log tail.

The Ops Console uses the same tokens as Admin but leans harder on `--font-mono` and the `--ink` neutral ramp; brand pink is reserved for "act now" affordances only (firing thresholds, retry buttons).
