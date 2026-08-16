using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;
using Rebus.Retry.Simple;
using Rebus.Topic;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Messaging.CloudEvents;
using SbaCars.BuildingBlocks.Messaging.Topology;
using SbaCars.BuildingBlocks.Messaging.Tracing;

namespace SbaCars.BuildingBlocks.Messaging;

/// <summary>
/// The single entry point every service uses to wire its Rebus bus (§6, D9) — the messaging
/// equivalent of <c>ObservabilityExtensions.AddSbaCarsObservability</c>: one call, from
/// <c>Program.cs</c>, that leaves the process with a fully configured, ready-to-start bus.
/// </summary>
public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="MessagingOptions"/> (bound and validated, <c>ValidateOnStart</c>, D6),
    /// the Rebus bus itself (RabbitMQ transport, retry strategy, topic convention, CloudEvents and
    /// tracing pipeline steps — D3, D7, D8), and <see cref="IIntegrationEventPublisher"/> (D5).
    /// </summary>
    /// <param name="serviceName">
    /// This process' logical name (e.g. <c>"inventory-service"</c>) — used as the Rebus bus name
    /// (shows up in logs/diagnostics), as the RabbitMQ client connection name prefix (so the
    /// broker's management UI can tell which process a connection belongs to), and as the CloudEvents
    /// <c>ce_source</c> value (D7: <c>urn:sbacars:{serviceName}</c>).
    /// </param>
    public static IServiceCollection AddSbaCarsMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        services.AddOptions<MessagingOptions>()
            .Bind(configuration.GetSection(MessagingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<MessagingOptions>, MessagingOptionsValidator>();

        services.AddRebus((configure, provider) =>
        {
            var options = provider.GetRequiredService<IOptions<MessagingOptions>>().Value;

            return configure
                .Transport(t => t.UseRabbitMq(options.ConnectionString, options.InputQueueName)
                    // D3: a single topic exchange for pub/sub plus a single direct exchange for
                    // point-to-point, instead of §6.3's "exchange per event type" — see the remarks
                    // on MessagingTopology for why that plan is not implementable on top of Rebus
                    // within this platform's connection budget, and what is preserved/traded away.
                    .ExchangeNames(MessagingTopology.DirectExchangeName, MessagingTopology.TopicExchangeName)
                    // §6.3.1: "concorrência se regula por prefetch, nunca abrindo mais conexões" —
                    // this, and MaxParallelism/NumberOfWorkers below, are the only knobs this
                    // topology allows for tuning throughput; opening additional connections is not
                    // one of them.
                    .Prefetch(options.PrefetchCount)
                    // Purely cosmetic on the wire, but the only way to tell, from the broker's
                    // management UI, which of the (at most) two connections a given process has
                    // opened belongs to its bus rather than to its readiness probe (see
                    // RabbitMqConnectionProbe's remarks on the same two-connections-per-process
                    // budget) or to another service instance entirely.
                    .SetConnectionName($"{serviceName}/{Environment.MachineName}")
                    .CustomizeConnectionFactory(factory =>
                    {
                        // §6.3.1: a managed broker (CloudAMQP in production) drops idle connections,
                        // so the heartbeat must be set explicitly rather than left at whatever
                        // RabbitMQ.Client's own default happens to be — and Rebus.RabbitMq's own
                        // builder exposes no setting for it, which is why this reaches into the
                        // concrete factory instead. ConnectionFactory (the concrete type
                        // Rebus.RabbitMq always constructs internally, per its own ConnectionManager)
                        // is the only implementation of IConnectionFactory that exposes
                        // RequestedHeartbeat/AutomaticRecoveryEnabled/TopologyRecoveryEnabled — if a
                        // future Rebus.RabbitMq version ever passed something else here, silently
                        // skipping this customization is a safer failure mode than crashing the boot
                        // over a cosmetic connection setting.
                        if (factory is ConnectionFactory concreteFactory)
                        {
                            concreteFactory.RequestedHeartbeat = TimeSpan.FromSeconds(options.HeartbeatSeconds);
                            concreteFactory.AutomaticRecoveryEnabled = true;
                            concreteFactory.TopologyRecoveryEnabled = true;
                        }

                        return factory;
                    }))
                .Options(o =>
                {
                    o.RetryStrategy(
                        options.EffectiveErrorQueueName,
                        options.MaxDeliveryAttempts,
                        secondLevelRetriesEnabled: options.SecondLevelRetriesEnabled);
                    o.SetNumberOfWorkers(options.NumberOfWorkers);
                    o.SetMaxParallelism(options.MaxParallelism);
                    o.SetBusName(serviceName);

                    // D4: routes by the event's business name (from [IntegrationEvent]), never by
                    // .NET type name — replaces Rebus' own DefaultTopicNameConvention.
                    o.Register<ITopicNameConvention>(_ => new IntegrationEventTopicConvention());

                    // D7/D8: CloudEvents envelope + W3C trace propagation, as our own pipeline steps
                    // rather than the Rebus.OpenTelemetry package — see CloudEventsOutgoingStep's
                    // remarks for why that package was evaluated and rejected. Both outgoing steps
                    // are anchored to run after SerializeOutgoingMessageStep, which is the first
                    // point in the send pipeline where the TransportMessage (and its header
                    // dictionary) exists to be stamped. The incoming step is anchored to run before
                    // DeserializeIncomingMessageStep, so the span is open even if deserialization
                    // itself fails.
                    o.Decorate<IPipeline>(c => new PipelineStepInjector(c.Get<IPipeline>())
                        .OnSend(new CloudEventsOutgoingStep(serviceName), PipelineRelativePosition.After, typeof(SerializeOutgoingMessageStep))
                        .OnSend(new TracingOutgoingStep(), PipelineRelativePosition.After, typeof(SerializeOutgoingMessageStep))
                        .OnReceive(new TracingIncomingStep(), PipelineRelativePosition.Before, typeof(DeserializeIncomingMessageStep)));
                });

            // No explicit .Logging(...) call here — a deliberate divergence from what a first
            // reading of the B1 task spec suggests. Rebus.ServiceProvider's own RebusInitializer
            // already wires Microsoft.Extensions.Logging into the bus automatically, once, after
            // this configure callback returns and before .Create() runs, by checking whether a
            // logger factory has already been registered and — if not — installing one built from
            // this process' own ILoggerFactory. There is no public MicrosoftExtensionsLogging()
            // extension method to call in the first place; the adapter class Rebus.ServiceProvider
            // uses for this is internal to that package. Calling .Logging(...) here would only be
            // relevant to override that default, which B1 has no reason to do.
        });

        services.AddTransient<IIntegrationEventPublisher, RebusIntegrationEventPublisher>();

        services.ConfigureOpenTelemetryTracerProvider((_, tracing) => tracing.AddSource(MessagingActivitySource.Name));

        return services;
    }
}
