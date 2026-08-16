namespace SbaCars.BuildingBlocks.Application;

/// <summary>
/// Persists whatever sensitive-data read accesses have been buffered for one request-scoped
/// persistence context (§5.7 of the foundation plan). The seam that lets
/// <c>SbaCars.BuildingBlocks.Web</c>'s end-of-request middleware guarantee a flush without
/// depending on EF Core or <c>DbContext</c>: <c>BuildingBlocks.Persistence</c> registers one
/// implementation per service's own <c>SbaCarsDbContext</c>, scoped so it resolves to the very
/// same instance the request's repositories read from.
/// </summary>
/// <remarks>
/// A no-op is an entirely valid outcome of <see cref="FlushAsync"/> — nothing was buffered
/// (the request never touched a sensitive entity), or the service has not wired a
/// sensitive-data audit interceptor at all yet. Both are expected steady states, not failures.
/// </remarks>
public interface ISensitiveDataAuditFlusher
{
    /// <summary>
    /// Flushes any pending sensitive-data audit entries. May throw if persisting them fails
    /// (e.g. a database error) — callers that must not let this fail the surrounding request
    /// (see <c>SensitiveDataAuditFlushMiddleware</c>) are responsible for catching and logging.
    /// </summary>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
