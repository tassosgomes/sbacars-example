namespace SbaCars.BuildingBlocks.Application;

/// <summary>
/// Buffers integration events during a use case so they can be published from inside the same
/// EF/Rebus outbox transaction as <see cref="IUnitOfWork.SaveChangesAsync"/> (§6.2). Implemented
/// by <c>EfUnitOfWork</c> when persistence and messaging are both registered; omitted by the
/// messaging-only no-op so B1 tests keep publishing directly to the broker.
/// </summary>
public interface IOutboxMessageStaging
{
    /// <summary>
    /// Queues an event to be published when the unit of work commits. Does not touch the broker yet.
    /// </summary>
    void Stage(object integrationEvent);
}
