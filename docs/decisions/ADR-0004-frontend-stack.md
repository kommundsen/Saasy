---
id: ADR-0004
type: adr
state: accepted
title: React + Vite for all three UI surfaces
date: 2026-05-03
deciders: [kim]
---

# ADR-0004 — React + Vite for all three UI surfaces

## Context

PRD §4.3 commits to three UIs: **Admin Dashboard** (Integrator-facing), **Customer Portal** (embeddable iframe AND white-label standalone, themable per Integrator), and **Ops Console** (Saasy-internal).

The Customer Portal's embed-and-white-label requirement is the deciding constraint:
- iframe embedding favors small self-contained bundles with first-class theming.
- White-label runtime theming (CSS variables, component overrides) is well-trodden in React.
- Blazor Server in iframes carries SignalR/cookie/cross-origin friction; Blazor WASM bundles are larger than React equivalents.

## Decision

Use **React + Vite** for all three UI surfaces.

- One frontend monorepo with shared component library (`@saasy/ui`) and a shared API client generated from the backend's OpenAPI spec.
- Each UI is its own Vite app: `admin/`, `portal/`, `ops/`, `ui/` (shared).
- Customer Portal builds as both `portal/standalone` (SPA) and `portal/embed` (iframe widget), sharing components.
- TypeScript everywhere. State: TanStack Query for server state; Zustand or Context for local UI state. Routing: TanStack Router.
- Component primitives via shadcn/ui for per-Integrator theming.
- Auth per Identity Mode (per [ADR-0007](ADR-0007-admin-dashboard-identity-and-customer-portal-posture.md)): Customer Portal is `IntegratorOwned` (signed JWT embed) or `Federated` (OIDC redirect to Integrator's IdP); Admin Dashboard uses ASP.NET Core Identity cookie auth with optional per-Integrator OIDC SSO.

## Consequences

- **Shared types, components, and API client across all three UIs.**
- **Loses .NET-typed-API dream.** TypeScript clients are generated from OpenAPI; the wire contract is the source of truth.
- **Two build pipelines** (.NET backend + Node/Vite frontend) — standard for any modern .NET + SPA stack.
- **Embed/standalone duality** is a build target rather than a runtime mode — clean separation.
