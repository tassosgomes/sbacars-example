using Microsoft.EntityFrameworkCore;
using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.BuildingBlocks.Persistence;

/// <summary>
/// Base repository over a service's own <see cref="SbaCarsDbContext"/>. Provides the handful of
/// operations that are identical for every aggregate — lookup by id, and staging an add/update/
/// removal for the next <see cref="EfUnitOfWork{TContext}.SaveChangesAsync"/> — so a concrete
/// repository only has to add the queries specific to its aggregate.
/// </summary>
/// <remarks>
/// Reads use <c>AsNoTracking</c> explicitly even though the context already defaults to
/// no-tracking (§4.2): a service is free to change that default per-query later, and a repository
/// that reads without tracking should not depend on a context-wide setting to stay correct.
/// Writes attach the entity to the change tracker explicitly, which is what "tracking explicit on
/// the write path" means in practice.
/// </remarks>
public abstract class Repository<TEntity>
    where TEntity : Entity
{
    protected SbaCarsDbContext Context { get; }

    protected DbSet<TEntity> Set { get; }

    protected Repository(SbaCarsDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Context = context;
        Set = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Set.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        // Flushing unconditionally (not just when `entity is ISensitiveDataEntity`) is what
        // makes this the one place a read-only caller doesn't have to know or care whether
        // TEntity happens to be sensitive: the flush itself is a no-op unless the context was
        // built with a SensitiveDataAuditInterceptor and something is actually pending (§5.7).
        await Context.FlushSensitiveDataAuditAsync(cancellationToken);

        return entity;
    }

    public virtual void Add(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Set.Add(entity);
    }

    public virtual void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Set.Update(entity);
    }

    public virtual void Remove(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Set.Remove(entity);
    }
}
