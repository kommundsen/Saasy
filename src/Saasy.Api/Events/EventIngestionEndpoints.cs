using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Saasy.Metering.Infrastructure.Ingestion;
using Saasy.Tenancy.Domain.Integrators;
using Saasy.Tenancy.Infrastructure.Auth;

namespace Saasy.Api.Events;

internal static class EventIngestionEndpoints
{
    internal static IEndpointRouteBuilder MapEventIngestionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/events", IngestEventsAsync)
            .WithName("IngestEvents")
            .RequireAuthorization()
            .WithSummary("Ingest one or a batch (max 500) of usage Events.")
            .WithDescription(
                "HTTP fallback ingestion path for low-volume Integrators (ADR-0001). " +
                "Accepts a single envelope object or a JSON array of up to 500 envelopes. " +
                "Returns 202 with per-event accepted/duplicate status.");

        return app;
    }

    private static async Task<IResult> IngestEventsAsync(
        HttpContext httpContext,
        [FromServices] IEventIngestionStrategy strategy,
        [FromServices] IngestRateLimiter rateLimiter,
        CancellationToken ct)
    {
        var integratorIdClaim = httpContext.User.FindFirstValue(ApiKeyClaims.IntegratorId);
        var tierClaim = httpContext.User.FindFirstValue(ApiKeyClaims.IntegratorTier);

        if (integratorIdClaim is null || !Guid.TryParse(integratorIdClaim, out var integratorId))
            return Results.Unauthorized();

        if (!Enum.TryParse<IntegratorTier>(tierClaim, out var tier))
            tier = IntegratorTier.Free;

        var cap = IntegratorTierLimits.IngestRequestsPerMinute(tier);
        if (!rateLimiter.TryConsume(integratorId, cap))
        {
            var retryAfter = rateLimiter.SecondsUntilWindowReset(integratorId);
            return Results.Extensions.TooManyRequests(retryAfter);
        }

        IReadOnlyList<EventEnvelope> envelopes;
        try
        {
            envelopes = await ParseEnvelopesAsync(httpContext.Request, ct);
        }
        catch (JsonException)
        {
            return BadRequest([new EnvelopeError(0, "body", "invalid_json")]);
        }

        if (envelopes.Count > 500)
            return BadRequest([new EnvelopeError(-1, "batchSize",
                $"Batch size {envelopes.Count} exceeds the maximum of 500.")]);

        if (envelopes.Count == 0)
            return BadRequest([new EnvelopeError(-1, "batchSize",
                "At least one envelope is required.")]);

        var shapeErrors = ValidateEnvelopes(envelopes);
        if (shapeErrors.Count > 0)
            return BadRequest(shapeErrors);

        var ingestResults = await strategy.IngestAsync(envelopes, ct);

        var eventResults = ingestResults
            .Select(r => new EventIngestResult(r.IdempotencyKey, r.Accepted, r.Reason))
            .ToList();

        return Results.Json(
            new BatchIngestResponse(eventResults),
            ResponseJsonOptions.SnakeCase,
            statusCode: StatusCodes.Status202Accepted);
    }

    private static IResult BadRequest(IReadOnlyList<EnvelopeError> errors)
        => Results.Json(
            new BatchErrorResponse(errors),
            ResponseJsonOptions.SnakeCase,
            statusCode: StatusCodes.Status400BadRequest);

    private static async Task<IReadOnlyList<EventEnvelope>> ParseEnvelopesAsync(
        HttpRequest request,
        CancellationToken ct)
    {
        using var doc = await JsonDocument.ParseAsync(request.Body, cancellationToken: ct);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            var list = new List<EventEnvelope>(root.GetArrayLength());
            foreach (var element in root.EnumerateArray())
            {
                var envelope = JsonSerializer.Deserialize<EventEnvelope>(
                    element.GetRawText(),
                    RequestJsonOptions.CaseInsensitive);
                if (envelope is not null)
                    list.Add(envelope);
            }
            return list;
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            var single = JsonSerializer.Deserialize<EventEnvelope>(
                root.GetRawText(),
                RequestJsonOptions.CaseInsensitive);
            return single is null ? [] : [single];
        }

        return [];
    }

    private static IReadOnlyList<EnvelopeError> ValidateEnvelopes(IReadOnlyList<EventEnvelope> envelopes)
    {
        var errors = new List<EnvelopeError>();

        for (var i = 0; i < envelopes.Count; i++)
        {
            var envelope = envelopes[i];

            if (envelope.IntegratorId == Guid.Empty)
                errors.Add(new EnvelopeError(i, "integrator_id", "required"));

            if (string.IsNullOrWhiteSpace(envelope.CustomerExternalRef))
                errors.Add(new EnvelopeError(i, "customer_external_ref", "required"));

            if (string.IsNullOrWhiteSpace(envelope.EventType))
                errors.Add(new EnvelopeError(i, "event_type", "required"));

            if (string.IsNullOrWhiteSpace(envelope.DimensionCode))
                errors.Add(new EnvelopeError(i, "dimension_code", "required"));

            if (string.IsNullOrWhiteSpace(envelope.IdempotencyKey))
                errors.Add(new EnvelopeError(i, "idempotency_key", "required"));

            if (envelope.OccurredAt == default)
                errors.Add(new EnvelopeError(i, "occurred_at", "required"));
        }

        return errors;
    }
}

// Request deserialization options: case-insensitive to match the snake_case envelope wire format.
file static class RequestJsonOptions
{
    internal static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

// Response serialization options: snake_case matches the envelope wire format,
// keeping request and response naming conventions consistent.
internal static class ResponseJsonOptions
{
    internal static readonly JsonSerializerOptions SnakeCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
}

// --- Response shapes ---

internal sealed record BatchIngestResponse(IReadOnlyList<EventIngestResult> Results);

internal sealed record EventIngestResult(
    string IdempotencyKey,
    bool Accepted,
    string? Reason);

internal sealed record BatchErrorResponse(IReadOnlyList<EnvelopeError> Errors);

internal sealed record EnvelopeError(int Index, string Field, string Reason);
