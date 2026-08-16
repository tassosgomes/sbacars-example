namespace SbaCars.BuildingBlocks.Application;

/// <summary>
/// Opens the transactional outbox session that must exist before an integration event is
/// published (§6.2). Use cases never call this directly — <see cref="IIntegrationEventPublisher"/>
/// ensures the session is open; <see cref="IUnitOfWork"/> completes and commits it on
/// <see cref="IUnitOfWork.SaveChangesAsync"/>.
/// </summary>
/// <remarks>
/// When no outbox is configured (messaging-only hosts, B1 tests), a no-op implementation is
/// registered so <see cref="IIntegrationEventPublisher"/> can keep the same call shape everywhere.
/// When persistence is registered, <c>EfUnitOfWork</c> also implements
/// <see cref="IOutboxMessageStaging"/>: <c>PublishAsync</c> stages the event, and the EF/Npgsql
/// transaction is opened later inside <c>SaveChangesAsync</c> so it can live entirely within one
/// execution-strategy invocation (required by <c>EnableRetryOnFailure</c>).
/// </remarks>
public interface IOutboxTransaction
{
    /// <summary>
    /// Ensures an outbox-backed database transaction is open for the current scope. Safe to call
    /// more than once — subsequent calls are no-ops until the unit of work commits or disposes.
    /// </summary>
    Task EnsureOpenAsync(CancellationToken cancellationToken = default);
}
