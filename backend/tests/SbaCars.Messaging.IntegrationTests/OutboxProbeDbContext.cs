using Microsoft.EntityFrameworkCore;
using SbaCars.BuildingBlocks.Domain;
using SbaCars.BuildingBlocks.Persistence;

namespace SbaCars.Messaging.IntegrationTests;

/// <summary>
/// Test-only aggregate for outbox integration tests — never shipped to a service.
/// </summary>
internal sealed class OutboxProbeEntity : Entity
{
    public string Name { get; private set; }

    public OutboxProbeEntity(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    private OutboxProbeEntity()
    {
        Name = string.Empty;
    }
}

/// <summary>
/// Targets the real <c>inventory</c> schema (outbox table lives there) with a probe table for
/// asserting commit/rollback alongside messaging.
/// </summary>
internal sealed class InventoryOutboxProbeDbContext : SbaCarsDbContext
{
    public const string Schema = "inventory";

    public DbSet<OutboxProbeEntity> Probes => Set<OutboxProbeEntity>();

    public InventoryOutboxProbeDbContext(DbContextOptions<InventoryOutboxProbeDbContext> options)
        : base(options, Schema)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OutboxProbeEntity>(entity =>
        {
            entity.ToTable("outbox_probe");
            entity.HasKey(probe => probe.Id);
            entity.Property(probe => probe.Name).IsRequired().HasMaxLength(200);
        });
    }
}
