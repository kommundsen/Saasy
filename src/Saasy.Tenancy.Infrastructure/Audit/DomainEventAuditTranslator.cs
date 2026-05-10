using System.Text.Json;
using Saasy.Tenancy.Application;
using Saasy.Tenancy.Domain;
using Saasy.Tenancy.Domain.Customers;
using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Infrastructure.Audit;

// Central translator: every domain event in the Tenancy context maps to an AuditEntry here.
// Keeping all audit translation in one place means each domain event's shape, action name,
// and payload projection are visible and auditable without tracing through multiple handlers.
public static class DomainEventAuditTranslator
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    public static AuditEntry? Translate(IDomainEvent domainEvent, string actor)
        => domainEvent switch
        {
            IntegratorCreated e => new AuditEntry(
                IntegratorId: e.IntegratorId,
                Actor: actor,
                Action: "integrator.created",
                TargetType: "Integrator",
                TargetId: e.IntegratorId.Value.ToString(),
                PayloadJson: JsonSerializer.Serialize(new
                {
                    name = e.Name,
                    kind = e.Kind.ToString(),
                    tier = e.Tier.ToString(),
                    timezone = e.Timezone,
                }, SerializerOptions),
                Timestamp: new DateTimeOffset(e.OccurredOn, TimeSpan.Zero)),

            IntegratorTierChanged e => new AuditEntry(
                IntegratorId: e.IntegratorId,
                Actor: actor,
                Action: "integrator.tier_changed",
                TargetType: "Integrator",
                TargetId: e.IntegratorId.Value.ToString(),
                PayloadJson: JsonSerializer.Serialize(new
                {
                    oldTier = e.OldTier.ToString(),
                    newTier = e.NewTier.ToString(),
                }, SerializerOptions),
                Timestamp: new DateTimeOffset(e.OccurredOn, TimeSpan.Zero)),

            ApiKeyMinted e => new AuditEntry(
                IntegratorId: e.IntegratorId,
                Actor: actor,
                Action: "integrator.api_key_minted",
                TargetType: "ApiKey",
                TargetId: e.ApiKeyId.Value.ToString(),
                // NEVER include plaintext or hash -- only ApiKeyId and last4
                PayloadJson: JsonSerializer.Serialize(new
                {
                    apiKeyId = e.ApiKeyId.Value.ToString(),
                    last4 = e.Last4,
                    name = e.Name,
                }, SerializerOptions),
                Timestamp: new DateTimeOffset(e.OccurredOn, TimeSpan.Zero)),

            ApiKeyRevoked e => new AuditEntry(
                IntegratorId: e.IntegratorId,
                Actor: actor,
                Action: "integrator.api_key_revoked",
                TargetType: "ApiKey",
                TargetId: e.ApiKeyId.Value.ToString(),
                PayloadJson: JsonSerializer.Serialize(new
                {
                    apiKeyId = e.ApiKeyId.Value.ToString(),
                }, SerializerOptions),
                Timestamp: new DateTimeOffset(e.OccurredOn, TimeSpan.Zero)),

            IngestionCredentialMinted e => new AuditEntry(
                IntegratorId: e.IntegratorId,
                Actor: actor,
                Action: "integrator.ingestion_credential_minted",
                TargetType: "IngestionCredential",
                TargetId: e.IngestionCredentialId.Value.ToString(),
                // NEVER include the plaintext SAS connection string
                PayloadJson: JsonSerializer.Serialize(new
                {
                    credentialId = e.IngestionCredentialId.Value.ToString(),
                }, SerializerOptions),
                Timestamp: new DateTimeOffset(e.OccurredOn, TimeSpan.Zero)),

            CustomerCreated e => new AuditEntry(
                IntegratorId: e.IntegratorId,
                Actor: actor,
                Action: "customer.created",
                TargetType: "Customer",
                TargetId: e.CustomerId.Value.ToString(),
                PayloadJson: JsonSerializer.Serialize(new
                {
                    externalRef = e.ExternalRef,
                    displayName = e.DisplayName,
                }, SerializerOptions),
                Timestamp: new DateTimeOffset(e.OccurredOn, TimeSpan.Zero)),

            CustomerRenamed e => new AuditEntry(
                IntegratorId: e.IntegratorId,
                Actor: actor,
                Action: "customer.renamed",
                TargetType: "Customer",
                TargetId: e.CustomerId.Value.ToString(),
                PayloadJson: JsonSerializer.Serialize(new
                {
                    newDisplayName = e.NewDisplayName,
                }, SerializerOptions),
                Timestamp: new DateTimeOffset(e.OccurredOn, TimeSpan.Zero)),

            _ => null,
        };
}
