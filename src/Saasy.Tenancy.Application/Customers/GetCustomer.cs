using Saasy.Tenancy.Domain.Customers;
using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Application.Customers;

public static class GetCustomer
{
    public sealed record Query(CustomerId CustomerId);

    public sealed record Result(
        CustomerId CustomerId,
        IntegratorId IntegratorId,
        string ExternalRef,
        string DisplayName,
        DateTime CreatedAt);

    public sealed class Handler(ICustomerRepository repository)
    {
        public async Task<Result?> HandleAsync(Query query, CancellationToken ct = default)
        {
            var customer = await repository.GetByIdAsync(query.CustomerId, ct);
            if (customer is null)
                return null;

            return new Result(
                customer.Id,
                customer.IntegratorId,
                customer.ExternalRef,
                customer.DisplayName,
                customer.CreatedAt);
        }
    }
}
