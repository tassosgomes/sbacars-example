using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Pipeline;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Messaging;
using SbaCars.BuildingBlocks.Messaging.Tracing;
using SbaCars.Contracts;

namespace SbaCars.Messaging.IntegrationTests;

/// <summary>
/// The "a publicação aparece no trace" half of B1's readiness criterion (§12, D8): publishing an
/// event through a real bus, over a real broker, produces a producer span and a consumer span that
/// share one trace-id, with the consumer span's parent being the producer span — and the W3C
/// <c>traceparent</c> that ties them together must have actually traveled inside the AMQP message,
/// not merely inside this process' own <see cref="Activity.Current"/> ambient state. Mirrors the
/// idiom of <c>SbaCars.Gateway.Tests.TraceContinuityTests</c>: an in-memory exporter capturing every
/// span this process' <see cref="TracerProvider"/> ends.
/// </summary>
/// <remarks>
/// The test event, <see cref="ProbeEvent"/>, is deliberately not a business event — those are B4
/// work, and <c>foundation.ping</c> end-to-end across two real services is B5. Its wire name,
/// <c>"test.messaging-probe"</c>, is chosen to be unmistakably not a Domain Docs event name.
/// </remarks>
[Collection(SbaCarsRabbitMqCollection.Name)]
public sealed class PublishTraceContinuityTests
{
    private readonly SbaCarsRabbitMqFixture _fixture;

    public PublishTraceContinuityTests(SbaCarsRabbitMqFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task APublishedEvent_ProducesAConsumerSpanChildOfThePublisherSpan_AndCarriesTheSameTraceparentOnTheWire()
    {
        var exportedActivities = new List<Activity>();
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(MessagingActivitySource.Name)
            .AddInMemoryExporter(exportedActivities)
            .Build();

        var queueName = MessagingTestConfiguration.UniqueQueueName("trace");
        var configuration = MessagingTestConfiguration.Build(_fixture, queueName);

        await using var host = await MessagingTestHost.StartAsync(services =>
        {
            services.AddSbaCarsMessaging(configuration, "trace-test");
            services.AddRebusHandler<ProbeEventHandler>();
        });

        var bus = host.Services.GetRequiredService<IBus>();
        await bus.Subscribe<ProbeEvent>();

        // Rebus.RabbitMq's Subscribe call declares the binding asynchronously against the broker;
        // give it a moment to land before publishing, or the very first publish can race the
        // binding and be silently undelivered.
        await Task.Delay(TimeSpan.FromMilliseconds(300));

        Activity? rootActivity;
        using (rootActivity = MessagingActivitySource.Instance.StartActivity("trace-continuity-test-root", ActivityKind.Internal))
        {
            var publisher = host.Services.GetRequiredService<IIntegrationEventPublisher>();
            await publisher.PublishAsync(new ProbeEvent());
        }

        var handled = await ProbeEventHandler.WaitForHandledAsync(TimeSpan.FromSeconds(10));
        handled.Should().BeTrue("the subscriber must actually receive and handle the published event");

        // The header the consumer saw on the wire, captured from inside the handler via
        // Rebus.Pipeline.MessageContext — proof the traceparent traveled in the AMQP message itself,
        // not merely survived inside this one process' in-memory Activity.Current.
        ProbeEventHandler.ObservedTraceparent.Should().NotBeNullOrEmpty();

        var publishSpan = exportedActivities.Should()
            .ContainSingle(activity => activity.OperationName == "test.messaging-probe publish")
            .Subject;
        var processSpan = exportedActivities.Should()
            .ContainSingle(activity => activity.OperationName == "test.messaging-probe process")
            .Subject;

        publishSpan.TraceId.Should().Be(rootActivity!.TraceId, "the publish span must be a child of the same trace the test started");
        processSpan.TraceId.Should().Be(rootActivity.TraceId, "the whole chain — root, publish, process — must share one trace-id");
        processSpan.ParentSpanId.Should().Be(publishSpan.SpanId, "the consumer span's parent must be the publisher span, not a fresh root");

        ProbeEventHandler.ObservedTraceparent.Should().Contain(publishSpan.TraceId.ToHexString());
        ProbeEventHandler.ObservedTraceparent.Should().Contain(publishSpan.SpanId.ToHexString());
    }

    [IntegrationEvent("test.messaging-probe")]
    public sealed class ProbeEvent;

    public sealed class ProbeEventHandler : IHandleMessages<ProbeEvent>
    {
        private static readonly TaskCompletionSource HandledSignal =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public static string? ObservedTraceparent { get; private set; }

        public Task Handle(ProbeEvent message)
        {
            ObservedTraceparent = MessageContext.Current.TransportMessage.Headers.GetValueOrDefault("traceparent");
            HandledSignal.TrySetResult();
            return Task.CompletedTask;
        }

        public static async Task<bool> WaitForHandledAsync(TimeSpan timeout)
        {
            var completed = await Task.WhenAny(HandledSignal.Task, Task.Delay(timeout));
            return completed == HandledSignal.Task;
        }
    }
}
