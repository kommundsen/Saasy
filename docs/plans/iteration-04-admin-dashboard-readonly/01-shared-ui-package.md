---
title: Shared UI package + design tokens
iteration: 04
status: todo
labels: [frontend, design-system]
depends-on: [00-react-vite-scaffold]
---

# Shared UI package + design tokens

`packages/ui` is the shared component library reused by Admin Dashboard, Customer Portal (Iteration 09), and Ops Console. Per-Integrator theming hook is stubbed but only the default theme ships now.

## Acceptance criteria

- `packages/ui` exports primitives: Button, Input, Table, Card, MoneyDisplay, Badge.
- Design tokens (color, spacing, typography) live in CSS variables; `ThemeProvider` swaps token values.
- Storybook (or equivalent) renders all primitives.
- `MoneyDisplay` formats `Money` per `Currency` minor units; uses `Intl.NumberFormat`.
