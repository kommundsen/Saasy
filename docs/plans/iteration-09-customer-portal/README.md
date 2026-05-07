# Iteration 09 — Customer Portal

## Goal

Ship the end-customer-facing portal in two build targets per [ADR-0004](../../decisions/ADR-0004-frontend-stack.md): an embeddable iframe and a white-label standalone SPA. Per-Integrator theming via CSS variables. Read-only views of usage, current Plan, recent Invoices.

## Out of scope

No write actions (no plan-change UI, no payment). Authentication uses placeholder integrator-issued tokens; full Identity Modes are Iteration 10.

## Exit criteria

- Two build targets from a single `apps/customer-portal` source: `embed` (iframe-friendly, postMessage handshake) and `standalone` (full hostname).
- Per-Integrator theme loaded by token claim or query param.
- Pages: current Plan, current-Period usage with rollup charts, recent Invoices (link to `.html` rendering).
- Authentication via short-lived integrator-signed JWT (HS256 with shared secret).
- Embed target sandboxes correctly: `Content-Security-Policy: frame-ancestors`, allowlist managed per-Integrator.

## Issues

- [ ] [00 — Customer Portal SPA scaffold (dual build targets)](00-portal-scaffold.md)
- [ ] [01 — Per-Integrator theming via CSS variables](01-theming.md)
- [ ] [02 — Integrator-signed JWT auth (placeholder identity mode)](02-jwt-auth-placeholder.md)
- [ ] [03 — Read-only usage + invoices views](03-readonly-views.md)
- [ ] [04 — iframe embed handshake + CSP](04-iframe-embed-handshake.md)
