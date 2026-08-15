namespace SbaCars.BuildingBlocks.Persistence.Auditing;

/// <summary>
/// One row of the access trail required by §5.7 of the foundation plan: who touched which
/// sensitive record, when, how (<see cref="SensitiveDataAccessType"/>) and which request it
/// happened in. Deliberately not a <c>SbaCars.BuildingBlocks.Domain.Entity</c> — this is a
/// persistence/audit concept with its own identity and lifecycle, not a business aggregate.
/// </summary>
/// <remarks>
/// Lives in the same schema as the entity it audits (mapped via
/// <see cref="SensitiveDataAuditModelBuilderExtensions.ConfigureSensitiveDataAudit"/>, which a
/// service opts into once it has its first <c>ISensitiveDataEntity</c>): §4.1 partitions by
/// schema, and a shared audit schema across services would reopen the exact cross-service
/// boundary A4 closes with database roles and grants.
/// </remarks>
public sealed class SensitiveDataAuditEntry
{
    public Guid Id { get; private set; }

    /// <summary>
    /// When the access happened, sourced from <c>IClock</c> — never <c>DateTime.UtcNow</c>
    /// directly (§4.2).
    /// </summary>
    public DateTimeOffset OccurredAt { get; private set; }

    public SensitiveDataAccessType AccessType { get; private set; }

    /// <summary>
    /// The CLR type name of the audited entity (e.g. <c>"JornadaDeCompraAssistida"</c>) — the
    /// "what" of "who read what".
    /// </summary>
    public string EntityName { get; private set; } = string.Empty;

    /// <summary>
    /// The audited entity's <c>Id</c>.
    /// </summary>
    public Guid EntityId { get; private set; }

    /// <summary>
    /// The caller's subject id from <c>ICurrentUser.UserId</c>. <see langword="null"/> when the
    /// access happened without an authenticated identity — recorded rather than suppressed,
    /// because an unauthenticated touch of sensitive data is itself worth being able to find.
    /// </summary>
    public string? UserId { get; private set; }

    /// <summary>
    /// The trace id of the request this access happened in (<see cref="System.Diagnostics.Activity.Current"/>,
    /// the same W3C id <c>BuildingBlocks.Web</c>'s correlation-id middleware tags onto the active
    /// span — §8), so this row can be matched against the log line and the trace of the same
    /// request. <see langword="null"/> outside of a traced request (e.g. a background job with no
    /// active <c>Activity</c>).
    /// </summary>
    public string? CorrelationId { get; private set; }

    // Required by EF Core materialization; kept private so nothing outside the ORM ever calls it.
    private SensitiveDataAuditEntry()
    {
    }

    public SensitiveDataAuditEntry(
        SensitiveDataAccessType accessType,
        string entityName,
        Guid entityId,
        string? userId,
        string? correlationId,
        DateTimeOffset occurredAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        Id = Guid.CreateVersion7();
        AccessType = accessType;
        EntityName = entityName;
        EntityId = entityId;
        UserId = userId;
        CorrelationId = correlationId;
        OccurredAt = occurredAt;
    }
}
