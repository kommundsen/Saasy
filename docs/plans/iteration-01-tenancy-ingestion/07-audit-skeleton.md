---
title: Audit skeleton
iteration: 01
status: todo
labels: [tenancy, audit, observability]
depends-on: [00-integrator-aggregate]
agent: backend
---

# Audit skeleton

A lightweight per-context `audit_log` table capturing actor, action, target, payload (JSONB), and timestamp. Wired into Tenancy mutations first; pattern reused in later iterations.

## Acceptance criteria

- `IAuditWriter` abstraction in `Saasy.Tenancy.Application`.
- Writes are part of the same EF Core `SaveChangesAsync` transaction as the domain mutation.
- `audit_log` table created in each context schema via per-context migration.
- Tenancy mutations (Integrator create, ApiKey mint/revoke, Customer create/rename) all write audit rows.
