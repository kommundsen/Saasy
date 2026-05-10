using Saasy.Tenancy.Domain.Customers;
using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Application.Customers;

public static class CreateCustomer
{
    public sealed record Command(IntegratorId IntegratorId, string ExternalRef, string DisplayName);

    public sealed record Result(CustomerId CustomerId);

    public sealed class Handler(ICustomerRepository repository, IUnitOfWork unitOfWork)
    {
        public async Task<Result> HandleAsync(Command command, CancellationToken ct = default)
        {
            var customer = Customer.Create(
                command.IntegratorId,
                command.ExternalRef,
                command.DisplayName);

            repository.Add(customer);
            await unitOfWork.CommitAsync(ct);

            return new Result(customer.Id);
        }
    }
}
