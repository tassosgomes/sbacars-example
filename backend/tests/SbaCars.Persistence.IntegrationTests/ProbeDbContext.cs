using Microsoft.EntityFrameworkCore;
using SbaCars.BuildingBlocks.Persistence;

namespace SbaCars.Persistence.IntegrationTests;

/// <summary>
/// Test-only <see cref="SbaCarsDbContext"/> mapping a single <see cref="ProbeEntity"/> to a
/// "probe" table. The schema is supplied by the caller so the very same context type can target
/// either "catalog" (positive path: DML as <c>svc_catalog</c>) or "inventory" (negative path:
/// <c>svc_catalog</c> must not reach it) without duplicating a context per schema.
/// </summary>
public sealed class ProbeDbContext : SbaCarsDbContext
{
    public DbSet<ProbeEntity> Probes => Set<ProbeEntity>();

    public ProbeDbContext(DbContextOptions<ProbeDbContext> options, string schema)
        : base(options, schema)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProbeEntity>(entity =>
        {
            entity.ToTable("probe");
            entity.HasKey(probe => probe.Id);
            entity.Property(probe => probe.Name).IsRequired().HasMaxLength(200);
        });
    }
}
