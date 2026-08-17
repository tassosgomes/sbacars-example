namespace SbaCars.BuildingBlocks.Messaging.Sagas;

/// <summary>
/// Schedules a durable timeout delivered back to this process' input queue (§2.5, B6). Sagas must
/// use this instead of <c>IBus.DeferLocal</c> when the PostgreSQL outbox is enabled: deferral via
/// the bus send pipeline is enlisted in the outbox transport and fails inside the saga receive
/// transaction.
/// </summary>
public interface ISagaTimeoutDeferral
{
    /// <summary>
    /// Persists a timeout in <c>{schema}.timeouts</c> for delivery to this service's input queue
    /// after <paramref name="delay"/>.
    /// </summary>
    Task DeferLocalAsync(TimeSpan delay, object message, CancellationToken cancellationToken = default);
}
