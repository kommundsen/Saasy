using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Application.Integrators;

public static class MintApiKey
{
    public sealed record Command(IntegratorId IntegratorId, string Name);

    public sealed record Result(ApiKeyId ApiKeyId, string PlaintextSecret);

    public sealed class Handler(IIntegratorRepository repository, IUnitOfWork unitOfWork)
    {
        public async Task<Result?> HandleAsync(Command command, CancellationToken ct = default)
        {
            var integrator = await repository.GetByIdAsync(command.IntegratorId, ct);
            if (integrator is null)
                return null;

            var (apiKey, plaintext) = integrator.MintApiKey(command.Name);

            await unitOfWork.CommitAsync(ct);

            return new Result(apiKey.Id, plaintext);
        }
    }
}
