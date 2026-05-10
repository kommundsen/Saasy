using Saasy.Tenancy.Application.Integrators;
using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Api.Integrators;

internal static class IntegratorEndpoints
{
    internal static IEndpointRouteBuilder MapIntegratorEndpoints(this IEndpointRouteBuilder app)
    {
        // Anonymous: Integrators must be able to mint their first ApiKey before any key
        // exists, so this endpoint cannot require auth. Future "create Integrator" endpoint
        // will also be anonymous for the same reason.
        app.MapPost("/v1/integrators/{id:guid}/api-keys", MintApiKeyAsync)
            .WithName("MintApiKey")
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> MintApiKeyAsync(
        Guid id,
        MintApiKeyRequest request,
        MintApiKey.Handler handler,
        CancellationToken ct)
    {
        var command = new MintApiKey.Command(new IntegratorId(id), request.Name);
        var result = await handler.HandleAsync(command, ct);

        if (result is null)
            return Results.NotFound();

        return Results.Ok(new MintApiKeyResponse(result.ApiKeyId.Value, result.PlaintextSecret));
    }
}

internal sealed record MintApiKeyRequest(string Name);

internal sealed record MintApiKeyResponse(Guid ApiKeyId, string PlaintextSecret);
