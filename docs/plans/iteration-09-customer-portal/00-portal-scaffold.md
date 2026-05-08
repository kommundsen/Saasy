---
title: Customer Portal SPA scaffold (dual build targets)
iteration: 09
status: todo
labels: [frontend, customer-portal]
depends-on: []
agent: frontend
---

# Customer Portal SPA scaffold

`apps/customer-portal` produces two build outputs from one Vite config: `embed/` and `standalone/`. Both reuse `packages/ui`.

## Acceptance criteria

- `pnpm build:embed` and `pnpm build:standalone` produce distinct outputs.
- Embed strips top navigation, exports a JS bootstrap that boots from a `<script>` tag with config attributes.
- Standalone is a full SPA with routing.
- Both surfaces share the same React component tree.
