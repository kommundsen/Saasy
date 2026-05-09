namespace Saasy.Tenancy.Application;

public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken ct = default);
}
