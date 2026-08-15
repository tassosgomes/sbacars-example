using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Persistence.IntegrationTests;

/// <summary>
/// Test-only aggregate, never shipped to a service. Its only purpose is to exercise
/// <see cref="Entity"/> through a real EF Core round trip: a new instance gets a fresh
/// <c>Guid.CreateVersion7()</c> id from its parameterless constructor, and the assertion that
/// matters is that reading it back from PostgreSQL returns that *same* id — proving EF Core
/// overwrites the constructor-generated id with the persisted value instead of leaving it as
/// materialization noise.
/// </summary>
public sealed class ProbeEntity : Entity
{
    public string Name { get; private set; }

    public ProbeEntity(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    // Required by EF Core materialization; kept private so nothing outside the ORM ever calls it.
    private ProbeEntity()
    {
        Name = string.Empty;
    }
}
