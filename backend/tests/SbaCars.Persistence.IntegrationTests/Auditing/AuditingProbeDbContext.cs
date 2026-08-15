using Microsoft.EntityFrameworkCore;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.BuildingBlocks.Persistence.Auditing;

namespace SbaCars.Persistence.IntegrationTests.Auditing;

/// <summary>
/// Test-only <see cref="SbaCarsDbContext"/> mapping both the sensitive
/// (<see cref="SensitiveProbeEntity"/>) and the plain (<see cref="ProbeEntity"/>) probe entities,
/// plus the opt-in <see cref="SensitiveDataAuditEntry"/> table — exactly the shape a real
/// service's <c>DbContext</c> would have the day it introduces its first sensitive entity.
/// </summary>
public sealed class AuditingProbeDbContext : SbaCarsDbContext
{
    public DbSet<SensitiveProbeEntity> SensitiveProbes => Set<SensitiveProbeEntity>();

    public DbSet<ProbeEntity> PlainProbes => Set<ProbeEntity>();

    public DbSet<SensitiveDataAuditEntry> AuditEntries => Set<SensitiveDataAuditEntry>();

    public AuditingProbeDbContext(
        DbContextOptions<AuditingProbeDbContext> options,
        string schema,
        SensitiveDataAuditInterceptor? sensitiveDataAuditInterceptor)
        : base(options, schema, sensitiveDataAuditInterceptor)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SensitiveProbeEntity>(entity =>
        {
            entity.ToTable("sensitive_probe");
            entity.HasKey(probe => probe.Id);
            entity.Property(probe => probe.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<ProbeEntity>(entity =>
        {
            entity.ToTable("plain_probe");
            entity.HasKey(probe => probe.Id);
            entity.Property(probe => probe.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.ConfigureSensitiveDataAudit();
    }
}
