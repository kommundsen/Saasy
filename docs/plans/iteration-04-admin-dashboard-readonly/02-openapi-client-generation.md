---
title: OpenAPI client generation pipeline
iteration: 04
status: todo
labels: [frontend, api]
depends-on: [00-react-vite-scaffold]
---

# OpenAPI client generation pipeline

Generate a typed TS client from the OpenAPI specs emitted by `Saasy.Api` into `packages/api-client`. Generation is a build step; manual edits forbidden.

## Acceptance criteria

- `pnpm generate:api` reads `openapi/*.yaml` and emits `packages/api-client/src/`.
- Generated code is git-tracked but with a header marking it generated.
- Admin Dashboard imports types and request fns from `@saasy/api-client`.
- CI fails if generation produces a diff against committed output.
