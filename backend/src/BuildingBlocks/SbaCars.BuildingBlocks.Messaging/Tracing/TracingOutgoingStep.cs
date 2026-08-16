using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Rebus.Messages;
using Rebus.Pipeline;
using SbaCars.BuildingBlocks.Messaging.Topology;

namespace SbaCars.BuildingBlocks.Messaging.Tracing;

/// <summary>
/// Starts the producer-side span for every outgoing message and propagates W3C trace context onto
/// the wire (§6.3.2, D8). Registered by <c>MessagingServiceCollectionExtensions.AddSbaCarsMessaging</c>
/// to run <b>after</b> <c>Rebus.Pipeline.Send.SerializeOutgoingMessageStep</c>, alongside
/// <c>CloudEventsOutgoingStep</c> — see that step's <c>&lt;remarks&gt;</c> for why
/// <c>Rebus.OpenTelemetry</c> was evaluated and rejected for this exact concern.
/// </summary>
public sealed class TracingOutgoingStep : IOutgoingStep
{
    public async Task Process(OutgoingStepContext context, Func<Task> next)
    {
        var transportMessage = context.Load<TransportMessage>();
        var headers = transportMessage.Headers;

        var messageType = context.Load<Message>()?.Body?.GetType();
        var topic = messageType is not null && IntegrationEventTopicConvention.TryGetTopic(messageType, out var resolvedTopic)
            ? resolvedTopic
            : "unknown";

        headers.TryGetValue(Headers.MessageId, out var messageId);

        // Started unconditionally, even when Activity.Current is null. This is precisely the case
        // Rebus.OpenTelemetry's own outgoing step gives up on (see CloudEventsOutgoingStep's
        // remarks): a publish issued from a background service or the outbox forwarder (B2/B7) has
        // no inbound-request Activity to be a child of. StartActivity below still produces a valid
        // *root* span in that case, which is what keeps a publish from a background process visible
        // in the trace at all, instead of silently vanishing.
        using var activity = MessagingActivitySource.Instance.StartActivity(
            $"{topic} publish",
            ActivityKind.Producer);

        if (activity is not null)
        {
            activity.SetTag("messaging.system", "rabbitmq");
            activity.SetTag("messaging.operation", "publish");
            activity.SetTag("messaging.destination.name", topic);

            if (messageId is not null)
            {
                activity.SetTag("messaging.message.id", messageId);
            }
        }

        // Even when nobody is listening on this ActivitySource (activity is null: no sampler has a
        // listener registered for it yet), inject whatever Activity.Current already carries — a
        // publish made from inside a sampled HTTP request should still carry that request's trace
        // context onto the wire, rather than losing correlation just because this specific span was
        // not itself created.
        var contextToPropagate = activity?.Context ?? Activity.Current?.Context ?? default;

        Propagators.DefaultTextMapPropagator.Inject(
            new PropagationContext(contextToPropagate, Baggage.Current),
            headers,
            static (carrier, key, value) => carrier[key] = value);

        await next();
    }
}
