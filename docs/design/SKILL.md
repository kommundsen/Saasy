---
name: Saasy Design System
description: Use when designing any Saasy surface (Admin Dashboard, Customer Portal, Ops Console) or any marketing/comms artefact for Saasy. Loads brand voice, vocabulary, color tokens, type system, and component conventions.
---

# Saasy Design System

Saasy is the metering-and-billing backend B2B SaaS products integrate with. Three product surfaces share this system: **Admin Dashboard** (Integrator-facing), **Customer Portal** (embed for end customers), **Ops Console** (Saasy internal).

## Always do first

1. Read `README.md` — full context, content fundamentals, and visual foundations.
2. Link `colors_and_type.css` from any new HTML file. It pulls Schibsted Grotesk + JetBrains Mono from Google Fonts and exposes every token.
3. Skim the relevant `ui_kits/<surface>/` for prebuilt chrome before building from scratch. The three kits implement the same tokens differently — match the right one.

## Non-negotiable vocabulary

Use the canonical domain words verbatim — `Integrator`, `Customer`, `Product Type`, `Dimension`, `Plan` / `Plan Version`, `Subscription`, `Event`, `Rollup`, `Threshold`, `Invoice`, `Line Item`, `Period Close`, `Final Close`. Never use `tenant`, `plan revision`, `environment`, `sub`, `metric`, or `record`. The full Avoid list is in the source `CONTEXT.md`.

## Visual cheat sheet

- **Brand pink** `--sass-pink (#E54D72)` — used sparingly. Primary buttons, the active nav rail, the brand mark. Never on body backgrounds, never as a wash behind data.
- **Ink ramp** `--ink → --ink-3` is the workhorse. All hierarchy comes from the neutral ramp; pink only for action.
- **Semantic states**: `--live` (green), `--warn` (amber), `--over` (red), `--sandbox` (purple). Each has `wash / base / ink` triplets.
- **Sandbox is a full chrome treatment**, not a badge — purple status bar across the top of the Admin Dashboard whenever an Integrator's `kind=sandbox`. Per ADR-0009, sandbox is a separate Integrator, not a toggle.
- **Numbers are mono.** Always. JetBrains Mono with `font-feature-settings: 'tnum'` for tabular figures. IDs, money, counts, percentages, timestamps.
- **Type scale**: display-1 (56px), h1 (32), h2 (24), h3 (16), body (15), ui (14), caption (12), mono-label (11 uppercase tracked).
- **4px spacing base.** Radii: 6, 8, 12, 16. Shadow ladder: `--shadow-1/2/3` (subtle).

## Components

- `.btn` (`.btn-primary`, `.btn-secondary`, `.btn-ghost`, `.btn-ink`)
- `.capsule` (`.live`, `.warn`, `.over`, `.sandbox`) — status pills
- `.quota` + `.quota-fill` (`.warn`, `.over`) — progress meter with optional threshold marker
- `.input` — focus halo uses `--sass-pink-tint`
- `.tbl` — hairline rows, mono numerics, hover tint
- `.kind-badge` (`.sandbox`, `.production`) — environment indicator
- `.live-dot` — pulsing dot for "live" capsules

## When you don't have an asset

- **Icons**: use Lucide subset rendered inline as 1.5px stroke / `currentColor` SVG. Manifest in `assets/icons/icon-manifest.json`. Never invent custom icons; if a metaphor is missing, ask.
- **Photography**: use `<image-slot>` placeholders — no AI imagery, no stock.
- **Customer logos**: render as the customer's initial in a 32×32 ink-square; replace with real assets when available.

## Per-surface posture

- **Admin Dashboard** (`ui_kits/admin/`): full chrome, sidebar nav, dense data tables, sandbox status bar when applicable. Brand pink on primary actions.
- **Customer Portal** (`ui_kits/portal/`): single-column, max 1080px, no sidebar, embed-friendly. Co-branded with the Integrator's mark in the page head plus a small "powered by Saasy" foot. End customers do not authenticate to Saasy directly.
- **Ops Console** (`ui_kits/ops/`): dark, mono-first, terminal-adjacent. Pink reserved for "act now" affordances (firing thresholds, retry buttons). Per ADR-0014 not held to customer-facing polish standards.

## Anti-patterns to refuse

- Hard quota enforcement, blocking modals, or "you're locked out" copy. Saasy is **Soft Enforcement only** (`docs/decisions/ADR-0012`). Threshold crossings notify; they never block.
- Generic "billing" iconography (credit cards, dollar signs as primary metaphors). Saasy doesn't take payments.
- Sandbox shown as a small toggle. It's a separate Integrator with separate data; the chrome must reflect that.
- Em-dashes typed as `—`. The source content uses ASCII `--` consistently.
