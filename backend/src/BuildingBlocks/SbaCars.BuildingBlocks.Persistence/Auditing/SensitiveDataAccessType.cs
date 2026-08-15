namespace SbaCars.BuildingBlocks.Persistence.Auditing;

/// <summary>
/// How a <see cref="SensitiveDataAuditEntry"/> row came to exist: whether the marked entity was
/// read (materialized by a query) or written (added, modified or removed and then saved).
/// </summary>
public enum SensitiveDataAccessType
{
    Read,
    Write,
}
