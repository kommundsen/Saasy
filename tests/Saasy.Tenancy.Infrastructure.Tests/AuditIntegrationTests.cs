using Microsoft.EntityFrameworkCore;
using Saasy.Tenancy.Application;
using Saasy.Tenancy.Application.Customers;
using Saasy.Tenancy.Application.Integrators;
using Saasy.Tenancy.Domain.Customers;
using Saasy.Tenancy.Domain.Integrators;
using Saasy.Tenancy.Infrastructure.Audit;

namespace Saasy.Tenancy.Infrastructure.Tests;

public sealed class AuditIntegrationTests
{
    private static TenancyDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TenancyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TenancyDbContext(options);
    }

    private static (TenancyDbContext db, TenancyUnitOfWork uow, TenancyAuditWriter auditWriter) CreateStack(
        string actor = "test:actor")
    {
        var db = CreateInMemoryDbContext();
        var auditWriter = new TenancyAuditWriter();
        var currentActor = new StubCurrentActor(actor);
        var uow = new TenancyUnitOfWork(db, auditWriter, currentActor);
        return (db, uow, auditWriter);
    }

    [Fact]
    public async Task MintApiKey_WritesExactlyOneAuditRow()
    {
        var (db, uow, _) = CreateStack();
        var integrator = Integrator.Create("Acme", IntegratorKind.Production, IntegratorTier.Free, Timezone.Create("UTC"));
        db.Integrators.Add(integrator);
        await db.SaveChangesAsync();
        integrator.ClearDomainEvents();

        var repo = new DbIntegratorRepository(db);
        var handler = new MintApiKey.Handler(repo, uow);
        await handler.HandleAsync(new MintApiKey.Command(integrator.Id, "my-key"));

        var auditRows = await db.AuditLogs.ToListAsync();
        Assert.Single(auditRows);
        Assert.Equal("integrator.api_key_minted", auditRows[0].Action);
    }

    [Fact]
    public async Task CreateCustomer_WritesExactlyOneAuditRow()
    {
        var (db, uow, _) = CreateStack();
        var repo = new DbCustomerRepository(db);
        var handler = new CreateCustomer.Handler(repo, uow);
        await handler.HandleAsync(new CreateCustomer.Command(IntegratorId.New(), "cust-001", "Acme Corp"));

        var auditRows = await db.AuditLogs.ToListAsync();
        Assert.Single(auditRows);
        Assert.Equal("customer.created", auditRows[0].Action);
    }

    [Fact]
    public async Task RenameCustomer_WritesExactlyOneAuditRow()
    {
        var (db, uow, _) = CreateStack();

        var customer = Customer.Create(IntegratorId.New(), "cust-001", "Acme Corp");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        customer.ClearDomainEvents();

        var repo = new DbCustomerRepository(db);
        var handler = new RenameCustomer.Handler(repo, uow);
        await handler.HandleAsync(new RenameCustomer.Command(customer.Id, "Renamed Corp"));

        var auditRows = await db.AuditLogs.ToListAsync();
        Assert.Single(auditRows);
        Assert.Equal("customer.renamed", auditRows[0].Action);
    }

    [Fact]
    public async Task CommitAsync_Actor_IsRecordedInAuditRow()
    {
        var (db, uow, _) = CreateStack(actor: "integrator:test-actor-id");
        var repo = new DbCustomerRepository(db);
        var handler = new CreateCustomer.Handler(repo, uow);
        await handler.HandleAsync(new CreateCustomer.Command(IntegratorId.New(), "cust-001", "Acme"));

        var auditRows = await db.AuditLogs.ToListAsync();
        Assert.Single(auditRows);
        Assert.Equal("integrator:test-actor-id", auditRows[0].Actor);
    }
}

// Adapters over the real DbContext for repository operations without going through full DI.
file sealed class DbIntegratorRepository(TenancyDbContext db) : IIntegratorRepository
{
    public Task<Integrator?> GetByIdAsync(IntegratorId id, CancellationToken ct = default)
        => db.Integrators.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExistsAsync(IntegratorId id, CancellationToken ct = default)
        => db.Integrators.AnyAsync(x => x.Id == id, ct);

    public void Add(Integrator integrator) => db.Integrators.Add(integrator);

    public Task<IReadOnlyList<Integrator>> GetByApiKeyPrefixAsync(string last4, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Integrator>>([]);
}

file sealed class DbCustomerRepository(TenancyDbContext db) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct = default)
        => db.Customers.FirstOrDefaultAsync(x => x.Id == id, ct);

    public void Add(Customer customer) => db.Customers.Add(customer);
}

file sealed class StubCurrentActor(string actor) : ICurrentActor
{
    public string Actor => actor;
}
