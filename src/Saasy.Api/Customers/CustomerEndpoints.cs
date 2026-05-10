using Saasy.Tenancy.Application.Customers;
using Saasy.Tenancy.Domain.Customers;
using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Api.Customers;

internal static class CustomerEndpoints
{
    internal static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/customers");

        group.MapPost("/", CreateCustomerAsync)
            .WithName("CreateCustomer");

        group.MapGet("/{id:guid}", GetCustomerAsync)
            .WithName("GetCustomer");

        group.MapPatch("/{id:guid}", RenameCustomerAsync)
            .WithName("RenameCustomer");

        return app;
    }

    private static async Task<IResult> CreateCustomerAsync(
        CreateCustomerRequest request,
        CreateCustomer.Handler handler,
        CancellationToken ct)
    {
        var command = new CreateCustomer.Command(
            new IntegratorId(request.IntegratorId),
            request.ExternalRef,
            request.DisplayName);

        var result = await handler.HandleAsync(command, ct);

        return Results.Created(
            $"/v1/customers/{result.CustomerId.Value}",
            new CreateCustomerResponse(result.CustomerId.Value));
    }

    private static async Task<IResult> GetCustomerAsync(
        Guid id,
        GetCustomer.Handler handler,
        CancellationToken ct)
    {
        var query = new GetCustomer.Query(new CustomerId(id));
        var result = await handler.HandleAsync(query, ct);

        if (result is null)
            return Results.NotFound();

        return Results.Ok(new CustomerResponse(
            result.CustomerId.Value,
            result.IntegratorId.Value,
            result.ExternalRef,
            result.DisplayName,
            result.CreatedAt));
    }

    private static async Task<IResult> RenameCustomerAsync(
        Guid id,
        RenameCustomerRequest request,
        RenameCustomer.Handler handler,
        CancellationToken ct)
    {
        var command = new RenameCustomer.Command(new CustomerId(id), request.DisplayName);
        var found = await handler.HandleAsync(command, ct);

        if (!found)
            return Results.NotFound();

        return Results.NoContent();
    }
}

internal sealed record CreateCustomerRequest(Guid IntegratorId, string ExternalRef, string DisplayName);
internal sealed record CreateCustomerResponse(Guid CustomerId);
internal sealed record CustomerResponse(Guid CustomerId, Guid IntegratorId, string ExternalRef, string DisplayName, DateTime CreatedAt);
internal sealed record RenameCustomerRequest(string DisplayName);
