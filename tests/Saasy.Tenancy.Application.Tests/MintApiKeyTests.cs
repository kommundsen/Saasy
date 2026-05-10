using Saasy.Tenancy.Application;
using Saasy.Tenancy.Application.Integrators;
using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Application.Tests;

public sealed class MintApiKeyTests
{
    private static Integrator CreateIntegrator() => Integrator.Create(
        name: "Acme Corp",
        kind: IntegratorKind.Production,
        tier: IntegratorTier.Free,
        timezone: Timezone.Create("UTC"));

    [Fact]
    public async Task HandleAsync_ReturnsPlaintextSecretAndApiKeyId()
    {
        var integrator = CreateIntegrator();
        var repo = new StubIntegratorRepository(integrator);
        var uow = new StubUnitOfWork();

        var handler = new MintApiKey.Handler(repo, uow);
        var command = new MintApiKey.Command(integrator.Id, "my-key");

        var result = await handler.HandleAsync(command);

        Assert.NotNull(result);
        Assert.NotNull(result.PlaintextSecret);
        Assert.False(string.IsNullOrEmpty(result.PlaintextSecret));
        Assert.NotEqual(default, result.ApiKeyId);
    }

    [Fact]
    public async Task HandleAsync_CommitsUnitOfWork()
    {
        var integrator = CreateIntegrator();
        var repo = new StubIntegratorRepository(integrator);
        var uow = new StubUnitOfWork();

        var handler = new MintApiKey.Handler(repo, uow);
        var command = new MintApiKey.Command(integrator.Id, "my-key");

        await handler.HandleAsync(command);

        Assert.True(uow.Committed);
    }

    [Fact]
    public async Task HandleAsync_WhenIntegratorNotFound_ReturnsNull()
    {
        var repo = new StubIntegratorRepository(null);
        var uow = new StubUnitOfWork();

        var handler = new MintApiKey.Handler(repo, uow);
        var command = new MintApiKey.Command(IntegratorId.New(), "my-key");

        var result = await handler.HandleAsync(command);

        Assert.Null(result);
    }
}

file sealed class StubIntegratorRepository(Integrator? integrator) : IIntegratorRepository
{
    public Task<Integrator?> GetByIdAsync(IntegratorId id, CancellationToken ct = default)
        => Task.FromResult(integrator);

    public Task<bool> ExistsAsync(IntegratorId id, CancellationToken ct = default)
        => Task.FromResult(integrator is not null);

    public void Add(Integrator i) { }

    public Task<IReadOnlyList<Integrator>> GetByApiKeyPrefixAsync(string last4, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Integrator>>([]);
}

file sealed class StubUnitOfWork : IUnitOfWork
{
    public bool Committed { get; private set; }

    public Task CommitAsync(CancellationToken ct = default)
    {
        Committed = true;
        return Task.CompletedTask;
    }
}
