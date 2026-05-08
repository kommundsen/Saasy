# Saasy Design System

> The brand and UI system for **Saasy** -- a backend service that B2B SaaS products integrate with to offload usage metering, plan/entitlement modeling, and Invoice generation.

## What is Saasy?

Saasy is the metering plumbing behind a B2B SaaS billing stack. **Integrators** (Saasy's customers, themselves SaaS companies) define their **Product Types** and **Plans** in Saasy, push **Events** as they happen via the hosted **Event Hub** or HTTP API, and Saasy:

- computes per-Customer **Rollups** (sum, max, time-weighted, unique-count),
- fires **Webhook** alerts on **Quota Threshold** crossings (Soft Enforcement only, never blocking),
- and produces ready-to-charge **Invoices** at each **Period Close**.

Payments and Invoice delivery remain the Integrator's responsibility in v1. Closest analogues: **Lago, Orb, Metronome**. Saasy's wedge is ergonomic, configurable Product Types and a strong embeddable Customer Portal out of the box.

### Three product surfaces

| Surface | Audience | Purpose |
|---|---|---|
| **Admin Dashboard** | Integrator engineers / RevOps | Configure Product Types, Plans, Customers, Subscriptions; inspect Rollups, Invoices, Webhooks; manage API keys |
| **Customer Portal** | The Integrator's end-customers | View their Subscription, current usage vs Quotas, past Invoices. Ships as both an embeddable iframe AND a white-label standalone SPA, themed per Integrator |
| **Ops Console** | Saasy's own team | Provision Integrators, monitor system health, audit logs, meter Saasy's own usage |

All three are React + Vite, share a `@saasy/ui` component library, and consume an OpenAPI-generated API client (per ADR-0004).

### Domain shape (vocabulary used throughout this system)

> Saasy is opinionated about words. Use these verbatim: `Integrator`, `Customer`, `Product Type`, `Dimension`, `Plan` / `Plan Version`, `Subscription`, `Event`, `Rollup`, `Threshold`, `Invoice`, `Line Item`, `Period Close`, `Final Close`. The `_Avoid:_` lists in `CONTEXT.md` (tenant, plan revision, environment, etc.) are not suggestions.

Cardinality at a glance:

```
Integrator
  +-- many Customers --- many Subscriptions -> one Plan Version -> one Plan
  +-- many Product Types --- many Dimensions
  +-- many Plans --- many Plan Versions --- many Pricing Components
                                              +-- Flat Fee
                                              +-- Per-Seat Fee  (time_weighted_last)
                                              +-- Metered Charge -> Quota + Overage
                                                                    tiered (graduated|volume)
```

---

## Sources

This design system was created from documentation only. There were no prior wireframes, Figma files, or existing UI to recreate. All visual decisions in this folder are first-pass design proposals derived from the product context. **Treat them as starting points, not ground truth.**

Source documents read while building this system (read-only mount path `Saasy/`):

- `Saasy/CONTEXT.md`: the canonical domain glossary; sets the vocabulary and the opinionated tone.
- `Saasy/CLAUDE.md`: top-level coding guidance.
- `Saasy/docs/plans/PRD.md`: product requirements, personas, phased delivery.
- `Saasy/docs/decisions/ADR-0004-frontend-stack.md`: React + Vite + shadcn/ui primitives + CSS-variable theming for the three UIs.
- `Saasy/docs/decisions/ADR-0009-...integrator-per-kind...`: sandbox vs production are separate Integrators (drives the **sandbox badge** affordance in chrome).
- `Saasy/docs/decisions/ADR-0010-period-boundaries-and-final-close.md`: Period Close vs Final Close; drives invoice timeline UI.
- `Saasy/docs/decisions/ADR-0012-threshold-firing-rules.md`: state-based Threshold firing; drives quota progress UI.
- `Saasy/docs/plans/iteration-04-admin-dashboard-readonly/`: pages and acceptance criteria for the Admin Dashboard.
- `Saasy/docs/plans/iteration-09-customer-portal/`: Customer Portal scope, theming, embed/standalone duality.

---

## Index

| Path | What's there |
|---|---|
| `README.md` | This file. Start here. |
| `colors_and_type.css` | The whole token layer: colors, type scale, spacing, radii, shadows, motion. Import this and you're 80% on-brand. |
| `SKILL.md` | Agent SKill manifest. Load this folder as a skill in Claude Code or other agent runtimes. |
| `assets/` | Logo, wordmark, brand marks, icon manifest. |
| `preview/` | Small HTML cards rendered in the project's Design System tab, one per token cluster / component cluster. |
| `ui_kits/admin/` | Admin Dashboard UI kit: JSX components + interactive `index.html`. |
| `ui_kits/portal/` | Customer Portal UI kit: embeddable + standalone variants. |
| `ui_kits/ops/` | Ops Console UI kit: Saasy-internal tooling chrome. |

---

## Content fundamentals

> Saasy's voice is **opinionated, precise, and dry-witted**. The `CONTEXT.md` glossary literally has an `_Avoid:_` line under each term banning competitor synonyms. Carry that confidence into every piece of UI copy.

### Tone

- **Confident, never apologetic.** "Final Close completes at 00:00 in your Timezone." Not "We'll try to close the period around midnight."
- **Specific, never woolly.** Use real domain nouns: `Rollup`, `Threshold`, `Late Event`, `Period Close`. Never "data point", "alert level", "cutoff".
- **Dry humor, sparingly.** The brand name is a pun (Sassy/SaaS) and the product knows it. Empty states can wink (*"No Rollups yet. The silence is deafening."*). Error states never do.
- **Second person ("you" / "your"), present tense.** "You haven't configured any Webhooks." Not "No webhooks have been configured."
- **First person plural ("we") only for system-attributable actions.** "We rejected this Event because its timestamp falls past Final Close." Reserved. Most copy is third-person about the system: "Saasy fires `usage.threshold.crossed` once per Threshold per Period."

### Casing

- **Domain nouns capitalized** when used as proper terms: `Integrator`, `Plan Version`, `Rollup`, `Threshold`, `Final Close`, `Invoice`. This isn't sentence-case grammar, it's a legibility convention so domain terms read as terms.
- **Sentence case** for headings, buttons, and field labels: "Create plan", not "Create Plan", unless "Plan" is itself acting as the proper-noun domain term in a phrase like "Plan Version".
- **UPPERCASE** sparingly: only for system-state badges (`SANDBOX`, `LIVE`, `FINAL CLOSE`, `OVERAGE`) and tabular column headers in the mono font. Letterspacing `0.06em`.
- **Code-like values** (`api_calls`, `time_weighted_last`, `usage.threshold.crossed`) always render in `JetBrains Mono`, never bold, never colored differently from body text.

### Numbers & units

- **Always render numbers in `JetBrains Mono` with tabular figures** in any UI showing usage, money, IDs, percentages, or timestamps. Numbers that read as numbers (1,234,567 events) align right; numbers that read as identifiers (`int_4F8a...`) align left.
- **Money:** code-prefixed, never symbol-only. `USD 1,240.00`, `EUR 980.50`. Saasy never converts between Currencies. Never imply it does.
- **Percentages:** integer where it reads naturally (`80%`), one decimal where precision matters (`80.4%`). Threshold defaults are `50%`, `80%`, `100%`, `110%`.
- **Timestamps:** ISO-8601 with explicit timezone in the chrome (`2026-05-08 14:32 UTC`). Period Close times explicitly note the Integrator's Timezone.

### Examples

**On-brand:**
- "Final Close lands in **23h 14m**. Late Events accepted until then; rejected after."
- "This Subscription is bound to **Pro v3** and has been since `2026-02-14`."
- "**THRESHOLD CROSSED**: `api_calls` is at 104% of Quota."
- *Empty state:* "No Webhook Subscriptions. Outbound notifications will go nowhere fast."

**Off-brand:**
- "Oops! Looks like you don't have any data yet" (apologetic, emoji-ready, vague).
- "Plan changed successfully" (wrong term, no specifics).
- "Period ending soon" (no time, no Timezone, no domain term).

### Emoji

**No emoji in product UI.** Status is communicated through color + glyph + label, never an emoji. Documentation-only contexts (changelogs, marketing copy) may use the brand mark glyph, never face/hand emoji.

---

## Visual foundations

### Colors

Saasy's palette is **two-axis**: a saturated **brand pink** (the only chromatic primary, leaning into the "Sassy" pun) plus a **deep ink-on-paper** chrome. Status is communicated through three semantic accents (live green, warning amber, overage red). No gradients in chrome. Gradients only appear as protection scrims under hero text or behind charts.

| Role | Token | Hex | Usage |
|---|---|---|---|
| Brand | `--sass-pink` | `#FF2D6F` | Primary buttons, links, the brand mark, active selection |
| Brand pressed | `--sass-pink-deep` | `#D8245C` | Pressed state on pink surfaces |
| Brand wash | `--sass-pink-wash` | `#FFF0F4` | Tint behind selected rows, brand-tinted info banners |
| Ink | `--ink` | `#0E0F13` | Primary fg on paper; primary surface in dark mode |
| Ink-2 | `--ink-2` | `#1B1D24` | Cards on ink, dark-mode raised surfaces |
| Paper | `--paper` | `#FAFAF7` | App background. Warm off-white, never pure white |
| Paper-2 | `--paper-2` | `#F2F2EC` | Recessed wells, code blocks, table headers |
| Fg-1 / fg-2 / fg-3 | grays | `#0E0F13` / `#5A5E6B` / `#8B8F9C` | Primary / secondary / tertiary text |
| Live | `--live` | `#14A05B` | Healthy state; in-period Rollups; Subscription active |
| Warn | `--warn` | `#F4A742` | Threshold approaching (>=80%); sandbox badge background |
| Over | `--over` | `#E5484D` | Overage; rejected events; final-close-passed errors |
| Steel | grays | `#E6E5DE` -> `#21232A` | Borders, hairlines, dividers |

Why this palette: every competitor in the metering space leans **blue** (Stripe, Orb, Metronome). Saasy goes **pink** to claim a distinct shelf, but pairs it with deep ink + warm paper so the chrome reads as serious infrastructure, not a consumer app.

### Typography

| Role | Family | Notes |
|---|---|---|
| Display & UI | **Schibsted Grotesk** (400, 500, 600, 700) | Geometric grotesk with character. Handles big numbers and condensed table headers without feeling neutral. Free, OFL, on Google Fonts. |
| Mono | **JetBrains Mono** (400, 500, 700) | Numbers, IDs, code, timestamps, command-style state badges. Tabular figures by default. Free, OFL, on Google Fonts. |

> **Note: there were no brand fonts in the source materials.** Both faces are free, well-supported, and self-hostable, picked deliberately to fit the Saasy character. See the **Font choice rationale** section below for why these two and what to swap to if the brand evolves.

Type scale (8-step):

```
display-1   72 / 76   -0.02em   600   "Final Close in 23h 14m"
display-2   56 / 60   -0.02em   600   marketing hero only
h1          40 / 44   -0.015em  600   page title
h2          28 / 32   -0.01em   600   section
h3          20 / 26   -0.005em  600   card title
body        15 / 22    0        400   default body
ui          14 / 20    0        500   buttons, labels, table cells
caption     12 / 16    0.02em   500   metadata, helper text
mono-num    14 / 20    0        500   numbers, IDs (JetBrains Mono, tabular)
mono-label  11 / 14    0.06em   600   UPPERCASE state badges (JetBrains Mono)
```

Body line-height is generous (1.45 to 1.5); table cells are tight (1.25). Letter-spacing is **negative** on display/heading sizes, **zero** on body, **slightly positive** on captions and mono labels.

### Spacing & rhythm

Saasy uses a **4px base** with a fibonacci-ish step ladder so spacing has personality at the larger end:

```
space-0   0
space-1   4
space-2   8
space-3   12
space-4   16
space-5   24
space-6   32
space-7   48
space-8   64
space-9   96
```

Density rules:
- **Tables and dense forms** snap to 8 / 12 / 16 paddings. The metering UIs are list-heavy and need to fit data.
- **Marketing surfaces and empty states** use 32 / 48 / 64. Comfortable, not airy.
- **Content max-width** for prose is **72ch**; for cards in dashboards, **1280px** content frame at the page level.

### Radii

```
radius-none   0       hairline rules, table cells
radius-sm     4       inputs, badges, tags
radius-md     8       buttons, small cards
radius-lg     12      cards, dialogs, the default
radius-xl     20      hero modules, marketing cards
radius-pill   999     status pills, capsule toggles
```

### Borders & dividers

Saasy uses **hairlines (1px)** as the primary divider. Never shadowed cards floating on shadowed cards. Border colors come from the steel ramp; the default is `--border-1: #E6E5DE` on paper, `#2B2D35` on ink. Dashed borders (`1px dashed`) only appear on **drop targets** and **placeholder slots** (e.g. "Add a Threshold").

### Shadows & elevation

Shadow system is minimal, three steps:

```
shadow-1   0 1px 0 rgba(14,15,19,0.04), 0 1px 2px rgba(14,15,19,0.04)         resting cards
shadow-2   0 4px 12px rgba(14,15,19,0.06), 0 2px 4px rgba(14,15,19,0.04)      hover, popovers
shadow-3   0 24px 48px rgba(14,15,19,0.12), 0 8px 16px rgba(14,15,19,0.06)    modals, command palette
```

No inner shadows. No glow. No colored shadows. Elevation comes from contrast and hairline, not depth.

### Backgrounds

- **No full-bleed photographic imagery** in product chrome. Saasy is a B2B infrastructure dashboard.
- **Marketing surfaces** may use a single grain-tinted off-white photo or a **dot-grid texture** (4px on 16px grid, `rgba(14,15,19,0.06)`).
- **Hero on marketing** uses a soft **brand-pink to paper radial wash** behind the headline. Never a hard linear gradient.
- **Code blocks and terminals** use `--ink` background with paper-tinted text. These are the only "dark" surfaces inside an otherwise paper UI.

### Motion

| Token | Value | Usage |
|---|---|---|
| `motion-1` | `120ms cubic-bezier(0.2, 0, 0.13, 1.5)` | Hover/press, popovers in. Slight overshoot on entry. |
| `motion-2` | `200ms cubic-bezier(0.2, 0, 0, 1)` | Most transitions: panel slides, drawer open, layout shifts. |
| `motion-3` | `400ms cubic-bezier(0.2, 0, 0, 1)` | Page-level transitions, data load fade-ins. |
| `motion-tick` | `1s linear infinite` | The metering "live" pulse: a `scaleY(0.6 to 1)` micro-pulse on the Live dot. The only ambient animation in the product. |

No bounces beyond the motion-1 micro-overshoot. No spring physics. **Easing leans short and sharp.** This is operational software.

### Hover, focus, press

- **Hover:** background shifts +2% toward ink (subtle) or -2% toward paper on dark surfaces. Pink primary buttons go to `--sass-pink-deep`. No scale on hover.
- **Focus:** **2px solid `--sass-pink`** outline at `2px` offset. Visible, never apologetic. We do not match focus styles to background.
- **Press:** `transform: scale(0.98)` + 50ms duration. Buttons only. Not on rows, cards, or links.

### Cards

Saasy's default card:
```
background: --paper
border: 1px solid --border-1
border-radius: 12px (--radius-lg)
shadow: --shadow-1 (very subtle, just enough to lift off the page background which is the same color)
padding: 24px
```

When stacked on `--paper-2`, cards shift to `--paper` to maintain contrast. We never use card-on-card with shadows; nested groupings use hairline-bordered sections inside one card.

### Capsules vs scrims

Saasy uses **hairline-bordered capsules** (rounded-pill, 1px steel border) for status badges, filter chips, and tag-like values. Capsules never have a fill color except for the four semantic states (live/warn/over/sandbox), which use a tinted background + matching-hue ink text. Never the full saturated hue as background with white text.

**Protection scrims** appear only over imagery or charts where text overlays: `linear-gradient(to top, rgba(14,15,19,0.6), transparent 60%)`.

### Transparency & blur

- **Backdrop blur** (`backdrop-filter: blur(12px)`) only on the global app shell top bar when content scrolls underneath. Nowhere else.
- **Semi-transparent fills** are reserved for the capsule status backgrounds (`color-mix(in oklch, --live 12%, transparent)` style).
- No glassmorphism. No frosted modals.

### Imagery vibe

If real photography appears (marketing only), it should be:
- **Operational.** Server racks, monitor scrubbing, hands at a keyboard, whiteboard ledger math, scoreboards, ticket stubs, taxi meters, freight scales, ledger paper. Anything that visually says "metering."
- **Warm.** Slight orange/sepia tint, not cool blue. No grayscale.
- **Slightly grainy.** Film-grain overlay at 4% opacity to keep textures alive. Saasy avoids the slick 4K-stock-photo aesthetic.

### Layout rules

- **Top app bar** is fixed (`64px`), `--paper` with hairline bottom border, `backdrop-filter: blur(12px)` when content scrolls under it.
- **Left sidebar** in Admin Dashboard is `240px` collapsed-by-default to `64px` on small viewports. Items are flat, no nested groups deeper than one level.
- **Page content** sits in a `1280px` max-width frame with `32px` horizontal padding on small screens.
- The **status row** below the app bar (current Integrator, Kind badge, environment indicator) is `40px` and persistent.

---

## Iconography

> Saasy uses **Lucide** as its icon set, sourced from CDN, with a curated subset documented in `assets/icons/icon-manifest.json`.
>
> **Substitution flag:** there is no icon set in the source codebase. Lucide is the closest match for the "thin, geometric, technical" aesthetic implied by the rest of the brand. If the team commissions a custom set, replace `assets/icons/`.

### Rules

- **Stroke-based, 1.5px stroke**, 24px viewbox by default. 16px in dense table cells; 20px in buttons.
- **Geometric, never illustrative.** Icons indicate categories and state, never decorate.
- **Currentcolor.** Icons inherit the surrounding text color. The only exception: the four state colors (live/warn/over/sandbox) which use the matching token directly.
- **One icon per metaphor, system-wide.** `webhook` is always the same Lucide glyph. The manifest lists the canonical mapping (Customer -> users, Plan -> layers, Rollup -> bar-chart-2, Webhook -> webhook, Threshold -> triangle-alert, Invoice -> file-text, etc.).
- **No emoji in product UI** (see Content Fundamentals).
- **No raster icons.** Everything is SVG.

### The Saasy mark

The brand mark is a **stylized asterisk-meets-clock** glyph: six tapered ticks radiating from a center, set in `--sass-pink`. It reads as both a counter and a punctuation mark, appropriate for metering and for the playful name. The full wordmark sets the mark immediately before "Saasy" in `Schibsted Grotesk 700`.

Variants in `assets/`:

- `logo-mark.svg`: mark only, square.
- `logo-wordmark.svg`: mark + "Saasy" wordmark.
- `logo-wordmark-mono.svg`: single-color (ink) version for monochrome contexts.
- `logo-wordmark-paper.svg`: paper version for ink backgrounds.

---

## Font choice rationale (no brand fonts existed)

The source materials contain no licensed brand fonts. These two were picked from Google Fonts to fit Saasy's character: **technical, opinionated, a touch playful, no consumer-app warmth.** Both are free under the SIL Open Font License and load via the import in `colors_and_type.css`.

### Display & UI: **Schibsted Grotesk** (primary recommendation)

- Geometric grotesk with **slightly humanist proportions** (open apertures, gentle curves on the `a`, `e`, `g`).
- Reads precise and confident at headline sizes; reads warm and legible at body sizes. Doesn't feel "design Twitter" the way Inter or Geist do, and avoids the flatness of Helvetica/Arial.
- Tabular numerics line up nicely with JetBrains Mono in mixed-content tables.
- Made by Schibsted (the Norwegian media group) for a real production system, so it ships with proper weights, metrics, and language support out of the box.
- **Why not Inter/Geist/Manrope:** overused; Saasy needs to look like itself, not like every other dev tool.

### Mono: **JetBrains Mono** (primary recommendation)

- Reads as "engineering software," which is exactly what Saasy is.
- Has all the ligatures + tabular figures + zero-with-slash needed for ID/timestamp/percentage rendering.
- Pairs cleanly with Schibsted Grotesk's grotesk skeleton.

### Alternatives if Schibsted Grotesk doesn't land

If you want to swap the display face, here are three drop-in alternatives that preserve the system's character. Each is one CSS-import edit away.

| Alternative | Vibe | Tradeoff |
|---|---|---|
| **Hanken Grotesk** | Slightly tighter, more neutral, reads more "software." | Less personality. Closer to category baseline. |
| **Bricolage Grotesque** | More expressive, slight optical-size variation, has soul at display sizes. | Personality is louder; reads more "design-led" than "infrastructure." |
| **Geist** (Vercel) | Trendy and clean, very technical. | Currently overused in dev-tool branding; risks blending in. |

For mono, **JetBrains Mono** is the strongest free option. Alternates: **IBM Plex Mono** (slightly warmer), **Geist Mono** (matched with Geist), **Commit Mono** (newer, has personality, but less ubiquitous).

### When to commission a real brand face

Once Saasy has a marketing budget and a brand designer, the right move is a **custom display face** for headlines (paid for once, used forever) and keep JetBrains Mono for code. The system here is structured so swapping `--font-sans` cascades cleanly. Until then, Schibsted Grotesk does the job.

---

---

## UI kits

Three surface-specific kits live in `ui_kits/`. Each has its own README and a runnable `index.html`.

| Kit | Path | Posture |
|---|---|---|
| **Admin Dashboard** | `ui_kits/admin/index.html` | Full app chrome — top bar, sidebar, sandbox status bar, dense tables. Demonstrates Subscriptions list, Subscription detail, Plans, Webhooks. |
| **Customer Portal** | `ui_kits/portal/index.html` | Embed-friendly single-column layout with co-branded header. Demonstrates usage overview, quota meters, period timeline, recent invoices. |
| **Ops Console** | `ui_kits/ops/index.html` | Dark mono-first internal tool. Demonstrates platform health, Integrator saturation, Late Event timeline, ingest tail, runbook callouts. |

All three import `colors_and_type.css` and use the same tokens. Differences in posture come from how those tokens are composed — same atoms, different rooms.

### Prototypes

Hi-fi click-through prototypes that exercise actual user flows (not just static screens). They live in `prototypes/`.

| Prototype | Path | Demonstrates |
|---|---|---|
| **Create a Plan** | `prototypes/admin-create-plan.html` | 3-step wizard (Basics → Pricing components → Review). Shows Plan Version v1 publishing, Pricing Component composition (Flat / Per-Seat / Metered with tiered Quota), success state with `plan.created` webhook. |
| **Approaching threshold** | `prototypes/portal-threshold.html` | 4-state scenario: healthy → 80% threshold crossed → upgrade modal → upgraded. Demonstrates Soft Enforcement copy ("you won't be cut off"), prorated Plan Version transition, `subscription.changed` webhook. |

---

## Caveats & next steps

- **No source visual assets existed.** Everything in this folder is a first-pass design proposal grounded in the product context, ADRs, and PRD. Not a recreation. Treat it as v0.1.
- **No brand fonts existed in the source.** Schibsted Grotesk + JetBrains Mono are picked from Google Fonts; both are free and SIL OFL. See **Font choice rationale** above for the reasoning and alternates.
- **Logo is brand-new.** The asterisk-clock mark and wordmark were designed for this system. Validate with stakeholders before adopting.
- **Color is a wedge bet.** Brand pink is a deliberate departure from the blue-dominant metering category. If marketing wants to converge with the category, swap `--sass-pink` for an indigo and the rest of the system holds.
- **Icons are Lucide, not custom.** Easy to swap out later.
