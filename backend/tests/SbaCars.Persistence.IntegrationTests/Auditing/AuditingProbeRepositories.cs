using SbaCars.BuildingBlocks.Persistence;

namespace SbaCars.Persistence.IntegrationTests.Auditing;

/// <summary>
/// Concrete <see cref="Repository{TEntity}"/> over <see cref="SensitiveProbeEntity"/> — exists
/// only so the test can exercise <c>FindAsync</c>'s automatic audit flush (§5.7) the same way a
/// real service repository would, instead of calling <c>SbaCarsDbContext.FlushSensitiveDataAuditAsync</c>
/// directly.
/// </summary>
public sealed class SensitiveProbeRepository : Repository<SensitiveProbeEntity>
{
    public SensitiveProbeRepository(SbaCarsDbContext context)
        : base(context)
    {
    }
}

/// <summary>
/// Concrete <see cref="Repository{TEntity}"/> over the unmarked <see cref="ProbeEntity"/> — the
/// negative-path counterpart used to prove reading a non-sensitive entity produces no audit noise.
/// </summary>
public sealed class PlainProbeRepository : Repository<ProbeEntity>
{
    public PlainProbeRepository(SbaCarsDbContext context)
        : base(context)
    {
    }
}
