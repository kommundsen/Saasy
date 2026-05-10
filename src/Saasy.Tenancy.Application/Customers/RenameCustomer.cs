using Saasy.Tenancy.Domain.Customers;

namespace Saasy.Tenancy.Application.Customers;

public static class RenameCustomer
{
    public sealed record Command(CustomerId CustomerId, string NewDisplayName);

    public sealed class Handler(ICustomerRepository repository, IUnitOfWork unitOfWork)
    {
        public async Task<bool> HandleAsync(Command command, CancellationToken ct = default)
        {
            var customer = await repository.GetByIdAsync(command.CustomerId, ct);
            if (customer is null)
                return false;

            customer.Rename(command.NewDisplayName);
            await unitOfWork.CommitAsync(ct);

            return true;
        }
    }
}
