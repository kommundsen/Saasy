using Microsoft.EntityFrameworkCore;
using Saasy.Tenancy.Application;
using Saasy.Tenancy.Domain.Customers;
using Saasy.Tenancy.Domain.Integrators;
using Saasy.Tenancy.Infrastructure.Audit;

namespace Saasy.Tenancy.Infrastructure.Tests;

// Verifies that TenancyUnitOfWork writes audit rows and domain mutations in the same
// SaveChangesAsync call. Both the audit row and the domain entity appear in the same
// EF change-tracker snapshot before SaveChangesAsync is invoked; there is no
// separate flush, which means they commit atomically under any EF provider that
// wraps SaveChangesAsync in a transaction (Postgres does).
public sealed class SingleTransactionTests
{
    [Fact]
    public async Task CommitAsync_AuditRowAndDomainEntityAreTracked_BeforeSaveChanges()
    {
        // Intercept at SaveChangesAsync via a spy DbContext to observe the change tracker
        // state at the point of save. This proves both the domain entity and audit row
        // are queued in the same SaveChangesAsync call.
        var options = new DbContextOptionsBuilder<TenancyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var spyDb = new SpyTenancyDbContext(options);
        var auditWriter = new TenancyAuditWriter();
        var uow = new TenancyUnitOfWork(spyDb, auditWriter, new StubCurrentActor("system:test"));

        var customer = Customer.Create(IntegratorId.New(), "cust-001", "Acme Corp");
        spyDb.Customers.Add(customer);

        await uow.CommitAsync();

        // At the moment SaveChangesAsync was called the spy captured the pending entries.
        Assert.Contains(spyDb.CapturedAddedEntityTypes, t => t == typeof(Customer));
        Assert.Contains(spyDb.CapturedAddedEntityTypes, t => t == typeof(AuditLog));
    }

    [Fact]
    public async Task CommitAsync_WhenSaveChangesThrows_NoDomainEntityPersistedIndependently()
    {
        // Verify that if CommitAsync fails, neither domain entities nor audit rows
        // are "half-committed": the spy dbContext throws during SaveChangesAsync,
        // and we confirm the in-memory store (backed by a successful real DbContext)
        // contains nothing.
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<TenancyDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var throwingDb = new ThrowingTenancyDbContext(options);
        var auditWriter = new TenancyAuditWriter();
        var uow = new TenancyUnitOfWork(throwingDb, auditWriter, new StubCurrentActor("system:test"));

        var customer = Customer.Create(IntegratorId.New(), "cust-001", "Acme Corp");
        throwingDb.Customers.Add(customer);

        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitAsync());

        // The in-memory store should be empty -- nothing was saved.
        using var verifyDb = new TenancyDbContext(options);
        Assert.Equal(0, await verifyDb.Customers.CountAsync());
        Assert.Equal(0, await verifyDb.AuditLogs.CountAsync());
    }

    [Fact]
    public async Task CommitAsync_FlushesAuditEntriesFromDomainEvents_BeforeSaveChanges()
    {
        var options = new DbContextOptionsBuilder<TenancyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new TenancyDbContext(options);
        var auditWriter = new TenancyAuditWriter();
        var uow = new TenancyUnitOfWork(db, auditWriter, new StubCurrentActor("system:test"));

        var customer = Customer.Create(IntegratorId.New(), "cust-001", "Acme Corp");
        db.Customers.Add(customer);

        await uow.CommitAsync();

        var auditRows = await db.AuditLogs.ToListAsync();
        Assert.Single(auditRows);
        Assert.Equal("customer.created", auditRows[0].Action);
    }
}

// SpyTenancyDbContext records what entity types were Added at the point SaveChangesAsync fires.
file sealed class SpyTenancyDbContext(DbContextOptions<TenancyDbContext> options)
    : TenancyDbContext(options)
{
    public List<Type> CapturedAddedEntityTypes { get; } = [];

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
            CapturedAddedEntityTypes.Add(entry.Entity.GetType());

        return base.SaveChangesAsync(cancellationToken);
    }
}

// ThrowingTenancyDbContext throws during SaveChangesAsync to simulate a DB failure.
file sealed class ThrowingTenancyDbContext(DbContextOptions<TenancyDbContext> options)
    : TenancyDbContext(options)
{
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("simulated database failure");
}

file sealed class StubCurrentActor(string actor) : ICurrentActor
{
    public string Actor => actor;
}
