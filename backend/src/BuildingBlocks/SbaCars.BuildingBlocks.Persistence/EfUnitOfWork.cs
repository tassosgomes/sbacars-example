using Microsoft.EntityFrameworkCore;
using SbaCars.BuildingBlocks.Application;

namespace SbaCars.BuildingBlocks.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IUnitOfWork"/>: commits whatever the service's own
/// <typeparamref name="TContext"/> is currently tracking. One instance per <c>TContext</c> per
/// service — registered by the service's own Infrastructure project, never here, because only
/// the service knows which concrete <see cref="SbaCarsDbContext"/> it owns.
/// </summary>
public sealed class EfUnitOfWork<TContext> : IUnitOfWork
    where TContext : SbaCarsDbContext
{
    private readonly TContext _context;

    public EfUnitOfWork(TContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
