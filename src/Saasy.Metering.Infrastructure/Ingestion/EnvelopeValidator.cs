using Microsoft.Extensions.Logging;

namespace Saasy.Metering.Infrastructure.Ingestion;

public static class EnvelopeValidator
{
    public static bool TryValidate(
        EventEnvelope envelope,
        ILogger logger,
        out string? failureReason)
    {
        if (envelope.IntegratorId == Guid.Empty)
        {
            failureReason = "integrator_id is missing or empty";
            logger.LogWarning("Poison event rejected: {Reason}", failureReason);
            return false;
        }

        if (string.IsNullOrWhiteSpace(envelope.CustomerExternalRef))
        {
            failureReason = "customer_external_ref is missing";
            logger.LogWarning("Poison event rejected: {Reason}", failureReason);
            return false;
        }

        if (string.IsNullOrWhiteSpace(envelope.EventType))
        {
            failureReason = "event_type is missing";
            logger.LogWarning("Poison event rejected: {Reason}", failureReason);
            return false;
        }

        if (string.IsNullOrWhiteSpace(envelope.DimensionCode))
        {
            failureReason = "dimension_code is missing";
            logger.LogWarning("Poison event rejected: {Reason}", failureReason);
            return false;
        }

        if (string.IsNullOrWhiteSpace(envelope.IdempotencyKey))
        {
            failureReason = "idempotency_key is missing";
            logger.LogWarning("Poison event rejected: {Reason}", failureReason);
            return false;
        }

        if (envelope.OccurredAt == default)
        {
            failureReason = "occurred_at is missing or zero";
            logger.LogWarning("Poison event rejected: {Reason}", failureReason);
            return false;
        }

        failureReason = null;
        return true;
    }
}
