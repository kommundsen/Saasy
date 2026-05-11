# Cross-iteration follow-ups

Backlog of work surfaced during iteration close-outs that doesn't fit the iteration that surfaced it. Each entry names the code that's currently stubbed or partial, the constraints that affect the implementation, and the iteration where it most naturally lands. None of these are blockers for the iteration they came from -- the iteration that surfaced them shipped with the stub / partial behaviour documented in the PR body.

## SAS minting split: local-dev shared-key, cloud real-SAS

**Surfaced in:** [iter-01/04 — Event Hubs provisioning](iteration-01-tenancy-ingestion/04-event-hubs-provisioning.md).

**Today:** `src/Saasy.Tenancy.Infrastructure/Integrators/StubIngestionCredentialMintService.cs` throws `NotImplementedException` on every call. The `Integrator.MintIngestionCredential(plaintext, encrypt)` aggregate method works -- it accepts a plaintext SAS connection string, encrypts it via the caller-supplied encryptor, and persists the ciphertext -- but nothing actually generates the plaintext.

**Split:**

- **Local dev:** a `DevIngestionCredentialMintService` returns the emulator's single baked-in connection string. The Event Hubs emulator has no management API ([Microsoft Learn: "It doesn't support on-the-fly management operations through a client-side SDK."](https://learn.microsoft.com/en-us/azure/event-hubs/overview-emulator)), so there is nothing real to "mint" locally -- every Integrator in dev gets the same `Endpoint=sb://...;SharedAccessKeyName=...;SharedAccessKey=...` the emulator already exposes via config.
- **Cloud (sandbox + production):** a `CloudIngestionCredentialMintService` uses `Azure.ResourceManager.EventHubs` to (a) create an `AuthorizationRule` scoped to `Send` only on the namespace or specific hub, named for the Integrator, (b) retrieve the resulting connection string via `RegenerateKeyAsync` or `GetKeysAsync`, (c) return the plaintext to the aggregate for encryption + persistence. The rule must be scoped to `Send` only -- never `Listen` or `Manage` -- so a leaked Integrator credential cannot be used to read other Integrators' Events or change the namespace.

**Constraint -- the cloud branch is only exercised in staging/prod.** Testcontainers cannot stand in for `Azure.ResourceManager.EventHubs` (there is no local Azure ARM emulator). Local unit / integration tests cover the dev branch and a mocked `IIngestionCredentialMintService`; the cloud branch is exercised by deployment smoke tests against a real sandbox subscription. Document this constraint in the service's code comments and in `docs/architecture/event-hubs.md`. Treat the cloud branch as "trust but verify in staging" -- any change to the cloud mint path goes through a staging deploy before prod.

**Probable iteration:** opportunistic -- can land any time after iter-02. Until the Tenancy Api needs to surface real SAS credentials to Integrators (which it doesn't yet, since no production usage path consumes them), the dev shared-key behaviour is sufficient.

## Distributed (Redis-backed) HTTP rate limiting

**Surfaced in:** [iter-01/06 — HTTP ingestion endpoint](iteration-01-tenancy-ingestion/06-http-event-ingestion.md).

**Today:** `src/Saasy.Metering.Infrastructure/Ingestion/IngestRateLimiter.cs` is a process-local fixed-window counter keyed by `IntegratorId`. Works correctly inside one Api instance; fails the moment a second instance comes up (each instance starts its own counter, so an Integrator effectively gets `cap * instanceCount` requests/minute).

**Approach:** swap the in-memory `ConcurrentDictionary` for a Redis-backed sliding-window or token-bucket implementation behind the same `IIngestRateLimiter` interface. The property tests we just wrote (`TryConsume_WithinCapAllTrueExceedCapFalse`, `TryConsume_DistinctIntegrators_CountsAreIndependent`, `SecondsUntilWindowReset_AfterAnyConsume_IsInBounds`) should pass unchanged -- the interface contract is what's tested, not the storage.

**Local-dev story:** Aspire's `AddRedis()` spins up a Redis container alongside Postgres, Event Hubs emulator, and Azurite. Local dev is identical to prod -- no Azure dependency for testing. Worker and Api both pick up the connection string via DI; no per-environment switching needed.

**Cloud story:** Azure Cache for Redis (Standard tier minimum for HA). Same connection-string mechanism via Aspire's azd-driven Bicep; no code changes between dev and cloud.

**Probable iteration:** [iter-05 — Quota, Overage & Thresholds](iteration-05-quota-overage-thresholds/) is the natural home -- it adds quota and threshold enforcement which sits on top of the same per-Integrator counter machinery, so a single Redis-backed rate-limit infrastructure can serve both the ingest cap and the threshold-firing logic.

## Local Event Hubs Capture sink

**Surfaced in:** [iter-01/04 — Event Hubs provisioning](iteration-01-tenancy-ingestion/04-event-hubs-provisioning.md), specifically the [ADR-0020](../decisions/ADR-0020-event-hubs-capture.md) requirement.

**Today:** Event Hubs Capture is configured on the cloud hub via `CaptureDescription` in `src/Saasy.AppHost/AppHost.cs`, writing Avro to Blob in production. The emulator does not support Capture ([Microsoft Learn confirms it's an emulator gap](https://learn.microsoft.com/en-us/azure/event-hubs/overview-emulator)), so locally there is no recoverable archive of inbound Events -- which means audit-replay flows can't be exercised end-to-end in dev.

**Approach:** add a `LocalEventCaptureWorker` BackgroundService in `Saasy.Worker` that runs only when `Environment.IsDevelopment()`. It reads from the `events` hub on a `local-capture` consumer group (parallel to `saasy-ingest`, never blocks or interferes with the rollup worker) and writes events to Azurite using the same path template ADR-0020 mandates for the cloud: `{Namespace}/{EventHub}/{PartitionId}/{Year}/{Month}/{Day}/{Hour}/{Minute}/{Second}`. Window: same 5-minute / 300 MB cadence as production Capture. Format: Avro for fidelity (matches the cloud schema exactly so audit-replay code can read both without branching) or JSONL.gz for ergonomics (simpler reader, looser fidelity). Pick Avro if iter-07's audit-replay code is provider-agnostic; pick JSONL.gz if dev ergonomics matter more than schema parity.

**Probable iteration:** [iter-07 — Invoices & Webhooks](iteration-07-invoices-webhooks/) -- this is the first iteration where Final Close reconciliation against raw inbound becomes auditable per ADR-0020, so the local capture infrastructure is what makes that iteration's tests runnable without real Azure.

## Migration up/down idempotence harness (Postgres support)

**Surfaced in:** iter-01 close-out test review.

**Today:** EF Core migrations across Tenancy (5 migrations) and Metering (1 migration) have auto-generated `Up()` / `Down()` pairs, but nothing tests that `Up -> Down -> Up` produces a byte-identical schema. The class of bug that bit iter-00 ("tenancy schema must exist before reading migrations history") is exactly what this would catch.

**Blocker:** [Conjecture.EFCore's `MigrationHarness.AssertUpDownIdempotentAsync`](https://ommundsen.dev/Conjecture/articles/reference/efcore.html#migrationharness) is the right test but v1 is SQLite-only. Our migrations target Postgres with schemas + JSONB + composite indices which SQLite can't faithfully replicate.

**Status:** upstream feature request filed at [kommundsen/Conjecture#691](https://github.com/kommundsen/Conjecture/issues/691) asking for Postgres support. Once the harness supports Postgres, add migration-symmetry tests as a one-`[Fact]`-per-DbContext addition to `Saasy.Tenancy.Infrastructure.Tests` and `Saasy.Metering.Infrastructure.Tests`.

**Probable iteration:** opportunistic, blocked on upstream. No iteration-level pressure to add it -- recent migrations have been simple `CreateTable`s with no `Down()`-related risk -- but worth pulling in as soon as Conjecture.EFCore v2+ lands.
