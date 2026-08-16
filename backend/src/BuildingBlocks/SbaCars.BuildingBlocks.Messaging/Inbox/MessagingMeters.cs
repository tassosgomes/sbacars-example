using System.Diagnostics.Metrics;

namespace SbaCars.BuildingBlocks.Messaging.Inbox;

/// <summary>
/// The single <see cref="Meter"/> the inbox deduplication step records on (§6.3). <see cref="Name"/>
/// is public and <see langword="const"/> because it is needed in two independent places that must
/// agree on the exact same string: the <c>AddMeter(MessagingMeters.Name)</c> call
/// <c>MessagingServiceCollectionExtensions.AddSbaCarsMessaging</c> makes against the host's
/// <c>MeterProviderBuilder</c> (otherwise counters are incremented but never exported), and any
/// test that attaches an in-memory exporter to observe them.
/// </summary>
public static class MessagingMeters
{
    public const string Name = "SbaCars.Messaging";

    private static readonly Meter Meter = new(Name);

    /// <summary>
    /// Incremented when an incoming message is discarded because <c>(message_id, consumer)</c> was
    /// already recorded in <c>{schema}.inbox_message</c> — either by the pre-handler EXISTS check or
    /// by a benign race on the post-handler INSERT (§6.3, B3).
    /// </summary>
    public static readonly Counter<long> InboxDuplicatesDiscarded =
        Meter.CreateCounter<long>(
            "messaging.inbox.duplicates_discarded",
            description: "Incoming messages discarded because (message_id, consumer) was already processed.");
}
