namespace SbaCars.BuildingBlocks.Messaging.Inbox;

/// <summary>
/// Persistence port for the per-service <c>{schema}.inbox_message</c> table (§6.3, B3). Not an EF
/// entity — the same raw-SQL approach as the Rebus outbox table (B2).
/// </summary>
public interface IInboxStore
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="messageId"/> was already processed by
    /// <paramref name="consumer"/>.
    /// </summary>
    Task<bool> IsProcessedAsync(string messageId, string consumer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a successfully handled message. Returns <see langword="false"/> when a concurrent
    /// delivery won the race (unique-key violation on <c>(message_id, consumer)</c>).
    /// </summary>
    Task<bool> TryRecordProcessedAsync(
        string messageId,
        string consumer,
        CancellationToken cancellationToken = default);
}
