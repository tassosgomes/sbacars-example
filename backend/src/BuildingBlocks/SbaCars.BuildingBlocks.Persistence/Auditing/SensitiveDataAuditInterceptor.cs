using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.BuildingBlocks.Persistence.Auditing;

/// <summary>
/// Records the access trail required by §5.7 of the foundation plan for every
/// <see cref="ISensitiveDataEntity"/>: writes via <see cref="SaveChangesInterceptor"/>, reads via
/// <see cref="IMaterializationInterceptor"/>. Registered per service with
/// <c>optionsBuilder.AddInterceptors(interceptor)</c> and handed to the same
/// <see cref="SbaCarsDbContext"/> instance so it can flush buffered reads — see
/// <see cref="SbaCarsDbContext.FlushSensitiveDataAuditAsync"/> for why a plain "audit on read"
/// cannot be fully automatic in EF Core.
/// </summary>
/// <remarks>
/// <para>
/// <b>What read auditing covers.</b> <see cref="IMaterializationInterceptor.InitializedInstance"/>
/// fires once for every row EF Core turns into a CLR instance of a mapped entity type — for a
/// tracked query, a no-tracking query, <c>Find</c>, eager-loaded navigations, all of it — as long
/// as the query's result shape <em>is</em> the marked entity type.
/// </para>
/// <para>
/// <b>What it does not cover, and cannot.</b> A LINQ projection — <c>.Select(x => new
/// { x.Cpf })</c>, a DTO projection, <c>.Select(x => x.Cpf)</c> — never materializes an instance of
/// the entity type at all; EF Core builds the projected shape directly from the data reader. No
/// entity instance means <see cref="IMaterializationInterceptor"/> is never invoked, so a
/// projection over a sensitive entity leaves <em>no audit trail</em>. The same is true of raw SQL
/// whose result is read as a scalar or a non-entity shape. This is a real, structural limitation of
/// intercepting materialization rather than the query itself — flagged here instead of pretending
/// projection is covered. Any repository or query that must expose sensitive fields through a
/// projection has to audit that access itself.
/// </para>
/// <para>
/// <b>Why reads cannot be persisted the instant they happen.</b>
/// <see cref="IMaterializationInterceptor.InitializedInstance"/> is a synchronous callback invoked
/// while the underlying <c>DbDataReader</c> is still open on the query's connection — issuing
/// another command against that same connection from inside the callback is not safe. Read audit
/// entries are therefore buffered per <see cref="DbContext"/> instance (keyed by object identity, so
/// one interceptor — which the EF Core docs describe as effectively a singleton — can safely serve
/// many contexts) and flushed as tracked <see cref="SensitiveDataAuditEntry"/> adds the next time
/// that context's own <c>SaveChangesAsync</c>/<c>SaveChanges</c> runs, or explicitly via
/// <see cref="SbaCarsDbContext.FlushSensitiveDataAuditAsync"/>. <see cref="Repository{TEntity}"/>
/// calls that flush after every read, which is what makes a pure read-only request (one that never
/// calls <c>SaveChanges</c> on its own) still end up with a persisted audit row.
/// </para>
/// </remarks>
public sealed class SensitiveDataAuditInterceptor : SaveChangesInterceptor, IMaterializationInterceptor
{
    private static readonly ConditionalWeakTable<DbContext, List<SensitiveDataAuditEntry>> PendingReads = [];

    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public SensitiveDataAuditInterceptor(ICurrentUser currentUser, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(clock);

        _currentUser = currentUser;
        _clock = clock;
    }

    /// <inheritdoc />
    public object InitializedInstance(MaterializationInterceptionData materializationData, object instance)
    {
        if (instance is ISensitiveDataEntity && materializationData.Context is { } context)
        {
            var pending = PendingReads.GetOrCreateValue(context);
            lock (pending)
            {
                pending.Add(CreateEntry(SensitiveDataAccessType.Read, instance));
            }
        }

        return instance;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is { } context)
        {
            AppendPendingAuditEntries(context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context)
        {
            AppendPendingAuditEntries(context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Drains any read accesses buffered for <paramref name="context"/> and stages a write-audit
    /// entry for every currently tracked <see cref="ISensitiveDataEntity"/> that is about to be
    /// added, modified or removed — all as tracked adds on <paramref name="context"/> itself, so
    /// whichever <c>SaveChanges</c> call runs next (this method does not call it) persists them
    /// together with whatever else that call was already going to persist.
    /// </summary>
    internal void AppendPendingAuditEntries(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (PendingReads.TryGetValue(context, out var pending))
        {
            List<SensitiveDataAuditEntry> drained;
            lock (pending)
            {
                if (pending.Count == 0)
                {
                    drained = [];
                }
                else
                {
                    drained = [.. pending];
                    pending.Clear();
                }
            }

            foreach (var readEntry in drained)
            {
                context.Add(readEntry);
            }
        }

        // Snapshot first: staging a SensitiveDataAuditEntry below adds a new tracked entry, which
        // would invalidate an in-progress enumeration of ChangeTracker.Entries().
        var sensitiveWrites = context.ChangeTracker.Entries()
            .Where(entry => entry.Entity is ISensitiveDataEntity &&
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var write in sensitiveWrites)
        {
            context.Add(CreateEntry(SensitiveDataAccessType.Write, write.Entity));
        }
    }

    private SensitiveDataAuditEntry CreateEntry(SensitiveDataAccessType accessType, object entity)
    {
        var entityId = entity is Entity domainEntity ? domainEntity.Id : Guid.Empty;

        return new SensitiveDataAuditEntry(
            accessType,
            entity.GetType().Name,
            entityId,
            _currentUser.IsAuthenticated ? _currentUser.UserId : null,
            Activity.Current?.Id,
            _clock.UtcNow);
    }
}
