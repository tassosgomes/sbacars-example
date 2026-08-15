namespace SbaCars.BuildingBlocks.Application;

/// <summary>
/// Commits the changes made to a service's persistence unit as a single transaction. The
/// concrete implementation (EF Core <c>DbContext</c>, outbox enlistment, etc.) lives in
/// Infrastructure/Persistence — Application only depends on this port.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists the pending changes and returns the number of affected records.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
