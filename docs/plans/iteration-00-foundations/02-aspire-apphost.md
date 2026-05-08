---
title: Aspire AppHost composition root
iteration: 00
status: todo
labels: [foundations, aspire]
depends-on: [00-solution-layout, 01-central-package-management]
agent: backend
---

# Aspire AppHost composition root

`Saasy.AppHost` is the .NET Aspire composition root. It wires Api + Worker + Postgres (and later Event Hubs / Service Bus emulators) for local dev. No domain logic ships in AppHost.

## Acceptance criteria

- `Saasy.AppHost` references `Saasy.Api` and `Saasy.Worker` via Aspire `AddProject`.
- `dotnet run --project src/Saasy.AppHost` opens the dev dashboard with both services Healthy.
- `/health` endpoint on Api returns 200.
- Worker logs a heartbeat every N seconds (placeholder).

## References

- [ADR-0002](../../decisions/ADR-0002-backend-stack.md)
