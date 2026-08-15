using Microsoft.EntityFrameworkCore;

namespace SbaCars.BuildingBlocks.Persistence.Auditing;

/// <summary>
/// Opt-in EF Core mapping for <see cref="SensitiveDataAuditEntry"/>. Not applied automatically by
/// <see cref="SbaCarsDbContext"/> for every service: today none of the four services has a single
/// <c>ISensitiveDataEntity</c>, and mapping an unused table into all four schemas would force a
/// migration nobody's model change actually requires. A service calls this from its own
/// <c>OnModelCreating</c> the same day it introduces its first sensitive entity (D03, then D04).
/// </summary>
public static class SensitiveDataAuditModelBuilderExtensions
{
    public const string TableName = "sensitive_data_audit";

    public static ModelBuilder ConfigureSensitiveDataAudit(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<SensitiveDataAuditEntry>(entity =>
        {
            entity.ToTable(TableName);
            entity.HasKey(auditEntry => auditEntry.Id);
            entity.Property(auditEntry => auditEntry.EntityName).IsRequired().HasMaxLength(200);
            entity.Property(auditEntry => auditEntry.UserId).HasMaxLength(200);
            entity.Property(auditEntry => auditEntry.CorrelationId).HasMaxLength(200);

            // Read side of the audit trail: "who read which entity" is answered by this index.
            entity.HasIndex(auditEntry => new { auditEntry.EntityName, auditEntry.EntityId });
        });

        return modelBuilder;
    }
}
