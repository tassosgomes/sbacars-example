using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Persistence.IntegrationTests.Auditing;

/// <summary>
/// Test-only entity marked <see cref="ISensitiveDataEntity"/>, the positive case for
/// <see cref="SensitiveDataAuditInterceptor"/>'s tests: reading or writing one of these must
/// produce an audit row. <see cref="ProbeEntity"/> (unmarked) remains the negative case.
/// </summary>
public sealed class SensitiveProbeEntity : Entity, ISensitiveDataEntity
{
    public string Name { get; private set; }

    public SensitiveProbeEntity(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    // Required by EF Core materialization; kept private so nothing outside the ORM ever calls it.
    private SensitiveProbeEntity()
    {
        Name = string.Empty;
    }
}
