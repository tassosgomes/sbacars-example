using Rebus.Messages;
using Rebus.Pipeline;

namespace SbaCars.BuildingBlocks.Messaging.Inbox;

/// <summary>
/// At-least-once delivery guard (§6.3, B3): discards redeliveries of the same
/// <see cref="Headers.MessageId"/> for this process' <paramref name="consumer"/> identity, and
/// records successful handling in <c>{schema}.inbox_message</c>.
/// </summary>
/// <remarks>
/// <para>
/// Registered by <c>MessagingServiceCollectionExtensions.AddSbaCarsMessaging</c> to run
/// <b>after</b> <c>Rebus.Pipeline.Receive.DeserializeIncomingMessageStep</c> — immediately before
/// handler dispatch. <see cref="Tracing.TracingIncomingStep"/> stays <b>before</b> deserialization
/// so a duplicate still gets a consumer span for the attempt; this step prevents the handler from
/// running when the effect was already applied.
/// </para>
/// <para>
/// <b>Process-then-record</b> (lost message is worse than a tiny duplicate crash window):
/// </para>
/// <list type="number">
/// <item>If <c>(message_id, consumer)</c> already exists → do <b>not</b> call <c>next()</c>, ACK
/// normally, increment <see cref="MessagingMeters.InboxDuplicatesDiscarded"/>.</item>
/// <item>Else <c>await next()</c>.</item>
/// <item>If <c>next()</c> throws → do <b>not</b> insert (retries / second-level / error queue must
/// still work).</item>
/// <item>If <c>next()</c> succeeds → INSERT; unique violation ⇒ treat as duplicate (benign race),
/// increment the counter, do not fail the ACK.</item>
/// </list>
/// <para>
/// <paramref name="consumer"/> is the <c>serviceName</c> passed to <c>AddSbaCarsMessaging</c>
/// (e.g. <c>"inventory-service"</c>), not a handler type name — fan-out requires the composite key
/// <c>(message_id, consumer)</c>, never <c>message_id</c> alone.
/// </para>
/// </remarks>
public sealed class InboxDeduplicationIncomingStep(IInboxStore store, string consumer) : IIncomingStep
{
    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        var transportMessage = context.Load<TransportMessage>();

        if (!transportMessage.Headers.TryGetValue(Headers.MessageId, out var messageId) ||
            string.IsNullOrWhiteSpace(messageId))
        {
            throw new InvalidOperationException(
                "Inbox deduplication requires Rebus' rbs2-msg-id header on every incoming message.");
        }

        if (await store.IsProcessedAsync(messageId, consumer).ConfigureAwait(false))
        {
            MessagingMeters.InboxDuplicatesDiscarded.Add(1);
            return;
        }

        try
        {
            await next().ConfigureAwait(false);
        }
        catch
        {
            // Handler failed — do not record. Rebus retry / error queue must see the failure again.
            throw;
        }

        var recorded = await store.TryRecordProcessedAsync(messageId, consumer).ConfigureAwait(false);
        if (!recorded)
        {
            MessagingMeters.InboxDuplicatesDiscarded.Add(1);
        }
    }
}
