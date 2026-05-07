---
title: Per-Integrator theming via CSS variables
iteration: 09
status: todo
labels: [frontend, customer-portal, design-system]
depends-on: [00-portal-scaffold]
---

# Per-Integrator theming

The `ThemeProvider` from `packages/ui` accepts a token map keyed by IntegratorId. Token map fetched at boot from `GET /v1/integrators/{id}/theme` (placeholder endpoint — full theming admin UI is post-v1).

## Acceptance criteria

- Token schema documented (colors, font family, logo URL, border radius).
- Default theme always applies if Integrator has no overrides.
- Logo + brand color reflect immediately in the rendered SPA.
- No FOUC: tokens applied before first paint.
