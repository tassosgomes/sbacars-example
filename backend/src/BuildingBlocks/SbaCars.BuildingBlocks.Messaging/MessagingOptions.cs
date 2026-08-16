using System.ComponentModel.DataAnnotations;

namespace SbaCars.BuildingBlocks.Messaging;

/// <summary>
/// The <c>Messaging</c> configuration section (§6, §6.3, §6.3.1 of the architecture plan): connection
/// and topology knobs for this process' single Rebus bus. Bound through the Options Pattern with
/// <c>ValidateOnStart</c> — see <see cref="MessagingOptionsValidator"/> for the rules a data
/// annotation cannot express, and <c>MessagingServiceCollectionExtensions.AddSbaCarsMessaging</c> for
/// how these values are threaded into Rebus' configuration spell.
/// </summary>
/// <remarks>
/// <b>Why mandatory, like <c>Persistence</c>, and not optional, like <c>Observability:OtlpEndpoint</c>.</b>
/// <c>ObservabilitySettings.OtlpEndpoint</c> is allowed to be absent because a service that never
/// exports a trace can still do its job — the dashboard is a convenience for the developer's inner
/// loop, not a dependency of the request path (see the remarks on
/// <c>ObservabilityExtensions.AddSbaCarsObservability</c>). A bus is different: every one of the four
/// services participates in the event-driven flows this platform is built around (§6) — inventory
/// publishing stock changes, catalog reacting to them, and so on — so a service that boots without a
/// working connection to RabbitMQ cannot fulfil its role in the topology at all, exactly like a
/// service that boots without a working Postgres connection. §4.4's rule applies the same way here:
/// a missing or malformed <c>Messaging</c> section is a deploy mistake, and the process must fail at
/// boot, not on the first attempt to publish.
/// </remarks>
public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    /// <summary>
    /// The AMQP connection string for the RabbitMQ broker (<c>amqp://</c> in every environment this
    /// repository covers locally; <c>amqps://</c> once a managed broker like CloudAMQP is in the
    /// picture — §6.3.1). Validated as an absolute URI with one of those two schemes by
    /// <see cref="MessagingOptionsValidator"/>.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// This service's own input queue name (e.g. <c>"inventory"</c>) — one per service, never one
    /// per event type (D3): a Rebus bus has exactly one input queue, and one bus per event type
    /// would multiply the broker connections this topology has to stay within (§6.3.1).
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string InputQueueName { get; set; } = string.Empty;

    /// <summary>
    /// This service's error queue name. <see langword="null"/>/empty is a deliberate, supported
    /// value: it defaults to <c>{InputQueueName}.error</c> (§6.3) via <see cref="EffectiveErrorQueueName"/>
    /// rather than requiring every environment to spell out a name that is mechanically derivable.
    /// </summary>
    public string? ErrorQueueName { get; set; }

    /// <summary>
    /// How many times Rebus attempts to deliver a message before moving it to the error queue.
    /// </summary>
    [Range(1, 100)]
    public int MaxDeliveryAttempts { get; set; } = 5;

    /// <summary>
    /// Whether second-level retries are enabled (§6.3): after <see cref="MaxDeliveryAttempts"/> first-level
    /// attempts, the message is dispatched once more wrapped in an <c>IFailed&lt;TMessage&gt;</c>,
    /// giving a handler the chance to react to the failure itself before the message is dead-lettered.
    /// </summary>
    public bool SecondLevelRetriesEnabled { get; set; } = true;

    /// <summary>
    /// Number of Rebus workers competing over the input queue.
    /// </summary>
    [Range(1, 32)]
    public int NumberOfWorkers { get; set; } = 1;

    /// <summary>
    /// Maximum number of messages processed in parallel across all workers.
    /// </summary>
    [Range(1, 256)]
    public int MaxParallelism { get; set; } = 5;

    /// <summary>
    /// Maximum number of unacknowledged messages RabbitMQ will deliver to this connection before
    /// waiting for acks (§6.3.1: "concorrência se regula por prefetch, nunca abrindo mais conexões" —
    /// this, not additional buses/connections, is the intended knob for tuning throughput).
    /// </summary>
    [Range(1, 1000)]
    public int PrefetchCount { get; set; } = 10;

    /// <summary>
    /// AMQP heartbeat interval, in seconds. §6.3.1: a managed broker (CloudAMQP in production) drops
    /// idle connections, so a heartbeat must be set explicitly rather than left at whatever the
    /// client's own default happens to be.
    /// </summary>
    [Range(5, 600)]
    public int HeartbeatSeconds { get; set; } = 60;

    /// <summary>
    /// <see cref="ErrorQueueName"/> when set, otherwise <c>{InputQueueName}.error</c> (§6.3). Reused
    /// by both <see cref="MessagingOptionsValidator"/> (rule 3) and
    /// <c>MessagingServiceCollectionExtensions.AddSbaCarsMessaging</c>, so the default-derivation
    /// logic exists in exactly one place.
    /// </summary>
    public string EffectiveErrorQueueName =>
        string.IsNullOrEmpty(ErrorQueueName) ? $"{InputQueueName}.error" : ErrorQueueName;
}
