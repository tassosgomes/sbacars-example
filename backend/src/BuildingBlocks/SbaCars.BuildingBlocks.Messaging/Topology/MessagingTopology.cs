namespace SbaCars.BuildingBlocks.Messaging.Topology;

/// <summary>
/// Names of the two RabbitMQ exchanges every service's bus is configured with (§6.3, D3 of the B1
/// task spec). Used by <c>MessagingServiceCollectionExtensions.AddSbaCarsMessaging</c> to configure
/// <c>Rebus.RabbitMq</c>'s <c>RabbitMqOptionsBuilder.ExchangeNames</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Deliberate deviation from §6.3.</b> §6.3 describes "exchange per event type; queue per
/// consumer/event pair" (e.g. a queue literally named <c>catalog.estoque.oferta-incluida</c>). That
/// is not implementable on top of Rebus within this platform's connection budget:
/// </para>
/// <list type="bullet">
/// <item>A Rebus bus has exactly <b>one</b> input queue. A queue per consumer/event pair would
/// require one bus instance per event type per service.</item>
/// <item><c>Rebus.RabbitMq</c> opens exactly one AMQP connection per bus (see
/// <c>ConnectionManager</c> in the Rebus.RabbitMq source: a single <c>_activeConnection</c> field,
/// lazily opened and reused). N buses would mean N connections, and §6.3.1 budgets the whole
/// topology — four services plus their readiness probes — against CloudAMQP's 40-connection plan
/// limit.</item>
/// </list>
/// <para>
/// <b>What is implemented instead:</b> one durable topic exchange, <see cref="TopicExchangeName"/>
/// (<c>"sbacars.events"</c>), with the routing key equal to the event's business name (e.g.
/// <c>"estoque.oferta-incluida"</c> — see <c>IntegrationEventTopicConvention</c>); one input queue per
/// <b>service</b> (not per event), with one binding per event type that service actually subscribes
/// to; and one error queue per service. Rebus' own point-to-point exchange (used for direct
/// send/reply, not for this platform's pub/sub events) is <see cref="DirectExchangeName"/>
/// (<c>"sbacars.direct"</c>).
/// </para>
/// <para>
/// <b>What is preserved and what is traded away.</b> Preserved: routing is still by the event's
/// business name, and each consuming service still only receives the event types it explicitly
/// binds to — nothing subscribes to noise. Traded away: without a physical queue per event type, a
/// slow consumer of one event type creates head-of-line blocking for every other event type routed
/// into the same service's single queue — a burst of slow-to-handle <c>X</c> messages delays
/// processing of unrelated, fast <c>Y</c> messages sitting behind them in the same queue. This is the
/// trade §6.3.1's connection budget forces, and it is why <c>MessagingOptions.PrefetchCount</c> and
/// <c>MessagingOptions.MaxParallelism</c> exist as the knobs for tuning concurrency instead of adding
/// more queues/connections.
/// </para>
/// </remarks>
public static class MessagingTopology
{
    /// <summary>
    /// Durable topic exchange all integration events are published to. Routing key = business name
    /// of the event (D4), not the .NET type name.
    /// </summary>
    public const string TopicExchangeName = "sbacars.events";

    /// <summary>
    /// Rebus' own point-to-point exchange, used for direct send/reply — distinct from
    /// <see cref="TopicExchangeName"/> so pub/sub traffic and point-to-point traffic never share a
    /// routing namespace.
    /// </summary>
    public const string DirectExchangeName = "sbacars.direct";
}
