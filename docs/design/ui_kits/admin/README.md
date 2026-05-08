# Saasy Admin Dashboard UI Kit

The Integrator-facing console. Configure Product Types, Plans, Customers, Subscriptions; inspect Rollups, Invoices, Webhooks; manage API keys.

Open `index.html` to see the interactive demo.

## Components

- `Shell.jsx` — top bar, sidebar, page frame
- `Sidebar.jsx` — primary nav with Lucide icons + counts
- `TopBar.jsx` — Integrator switcher, Kind badge, search, user menu
- `KindBadge.jsx` — sandbox / production indicator (drives the chrome)
- `Capsule.jsx` — status pills (live, warn, over, sandbox, etc)
- `QuotaMeter.jsx` — usage progress with threshold marker + overage hatch
- `DataTable.jsx` — table with mono numerics, hairline rows, hover tint
- `PageHeader.jsx` — h1 + breadcrumb + actions
- `StatCard.jsx` — display-1 number cards
- `PeriodTimeline.jsx` — Period start / now / Period Close / Final Close

## Screens (in `index.html`)

1. **Subscriptions list** (default) — current Subscriptions, MTD spend, state.
2. **Subscription detail** — Rollups, threshold history, period timeline.
3. **Plans** — Plan / Plan Version tree.
4. **Webhooks** — outbound subscriptions and recent deliveries.
