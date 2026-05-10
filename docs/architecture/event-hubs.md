# Event Hubs -- partition key choice and capacity assumptions

## Partition key

Partition key = `integrator_id`.

**Why.** Event Hubs guarantees ordering within a partition. The business requirement is per-Integrator FIFO: all Events from a given Integrator must be processed in the order they arrived, because the Rollup worker applies them sequentially to accumulate usage totals. Cross-Integrator ordering is irrelevant.

Using `integrator_id` as the partition key means every Event from Integrator A lands in exactly one partition, and the Rollup worker for that Integrator reads from that partition in sequence. This gives us per-Integrator ordering without any application-level sequencing logic.

**Hot-partition risk.** A noisy Integrator (sustained high event volume) concentrates all its traffic on one partition. The mitigation layers are:

1. Integrator Tier carries an ingestion ceiling (Events/sec). Exceeding it triggers a 429 at the API edge before the Event reaches Event Hubs.
2. Partition count is sized for sustained throughput across the expected number of Integrators (see below). A single hot partition does not starve others because Event Hubs partitions are independent streams.
3. Rollup workers scale out per ADR-0019 (KEDA on partition lag). A hot partition will drive more worker replicas, which is correct -- the Integrator paying for more throughput gets more processing capacity.

## Capacity assumptions (v1 design-partner load)

| Parameter | Value | Notes |
|---|---|---|
| Throughput Units (TUs) | 2 | Handles 2 MB/s ingress, 4 MB/s egress. At ~1 KB/Event avg, this is ~2,000 Events/sec ingress. |
| Partitions | 4 | Each partition independently handles up to ~500 Events/sec sustained at the TU ceiling. 4 partitions cover ~4 active Integrators at sustained throughput or more at bursty loads. |
| Events/sec per Integrator (sustained) | 1,000 | Per ADR-0001. |
| Events/sec per Integrator (burst) | 10,000 | Accommodated by Event Hubs buffering within the partition; the Rollup worker catches up asynchronously. |
| Retention | 7 days | Default; expandable to 90. Long-term archive via Capture per ADR-0020. |

**Scaling path.** Partition count can only be increased on namespace recreation (Event Hubs does not support adding partitions to an existing hub). The 4-partition start is conservative. When Integrator count grows past the point where hot-partition contention is measurable (observable via Event Hubs metrics + Application Insights), add partitions via a namespace migration. TUs scale independently and can be adjusted in minutes.

## Capture

Capture is enabled on the `events` hub from Iter 01 per ADR-0020. Format is Avro. Window: 5 minutes / 300 MB. Destination: the per-environment Blob Storage container (`event-capture-sandbox` or `event-capture-prod`), supplied as an Aspire parameter (`eventHubsCaptureContainer`) so the same Bicep template serves both environments. Emitter writes are not blocked by Capture; it is a background copy managed by the Event Hubs service.

## Per-Integrator SAS credentials

Each Integrator holds one or more `IngestionCredential` Child Entities. The aggregate-side pattern is in place: `Integrator.MintIngestionCredential(plaintextConnectionString, encrypt)` receives a plaintext SAS connection string from the caller, encrypts it using the supplied `Func<string, string>` (backed by ASP.NET Core Data Protection in production), stores only the ciphertext in the `EncryptedConnectionString` column, and returns the plaintext to the caller exactly once. The plaintext is never written to any Postgres column or log.

The SAS policy is `Send`-only. The Integrator cannot read other Integrators' Events -- isolation is enforced by the SAS policy scope and the partition key, not by separate namespaces.

**Current state -- minting not yet live.** The actual acquisition of the SAS connection string from the Event Hubs management API (via `Azure.ResourceManager.EventHubs` -- `AuthorizationRuleResource.RegenerateKeyAsync`) is not yet implemented. `IIngestionCredentialMintService` in the Infrastructure layer has a stub (`StubIngestionCredentialMintService`) that throws `NotImplementedException` loudly so no accidental production use is possible. Implementing the real mint service is tracked as a follow-up issue in iter-02 or later.
