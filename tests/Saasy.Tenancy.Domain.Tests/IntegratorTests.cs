using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Domain.Tests;

public sealed class IntegratorTests
{
    [Fact]
    public void Create_WithValidArguments_ReturnsIntegratorWithExpectedProperties()
    {
        var integrator = Integrator.Create(
            name: "Acme Corp",
            kind: IntegratorKind.Production,
            tier: IntegratorTier.Free,
            timezone: Timezone.Create("Europe/Oslo"));

        Assert.NotEqual(Guid.Empty, integrator.Id.Value);
        Assert.Equal("Acme Corp", integrator.Name);
        Assert.Equal(IntegratorKind.Production, integrator.Kind);
        Assert.Equal(IntegratorTier.Free, integrator.Tier);
        Assert.Equal("Europe/Oslo", integrator.Timezone.IanaName);
        Assert.Equal(0u, integrator.Version);
    }

    [Fact]
    public void Create_EmitsIntegratorCreatedDomainEvent()
    {
        var integrator = Integrator.Create(
            name: "Acme Corp",
            kind: IntegratorKind.Sandbox,
            tier: IntegratorTier.Free,
            timezone: Timezone.Create("UTC"));

        var domainEvent = Assert.Single(integrator.DomainEvents);
        var created = Assert.IsType<IntegratorCreated>(domainEvent);
        Assert.Equal(integrator.Id, created.IntegratorId);
    }

    [Fact]
    public void ChangeTier_EmitsIntegratorTierChangedDomainEvent()
    {
        var integrator = Integrator.Create(
            name: "Acme Corp",
            kind: IntegratorKind.Production,
            tier: IntegratorTier.Free,
            timezone: Timezone.Create("UTC"));

        integrator.ClearDomainEvents();
        integrator.ChangeTier(IntegratorTier.Paid);

        Assert.Equal(IntegratorTier.Paid, integrator.Tier);
        var domainEvent = Assert.Single(integrator.DomainEvents);
        var tierChanged = Assert.IsType<IntegratorTierChanged>(domainEvent);
        Assert.Equal(integrator.Id, tierChanged.IntegratorId);
        Assert.Equal(IntegratorTier.Free, tierChanged.OldTier);
        Assert.Equal(IntegratorTier.Paid, tierChanged.NewTier);
    }

    [Fact]
    public void ChangeTier_WithSameTier_EmitsNoDomainEvent()
    {
        var integrator = Integrator.Create(
            name: "Acme Corp",
            kind: IntegratorKind.Production,
            tier: IntegratorTier.Free,
            timezone: Timezone.Create("UTC"));

        integrator.ClearDomainEvents();
        integrator.ChangeTier(IntegratorTier.Free);

        Assert.Empty(integrator.DomainEvents);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithInvalidName_Throws(string? name)
    {
        Assert.Throws<ArgumentException>(() =>
            Integrator.Create(
                name: name!,
                kind: IntegratorKind.Production,
                tier: IntegratorTier.Free,
                timezone: Timezone.Create("UTC")));
    }
}
