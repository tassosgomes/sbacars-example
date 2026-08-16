using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Rebus.Messages;
using Rebus.Pipeline;
using SbaCars.BuildingBlocks.Messaging.CloudEvents;

namespace SbaCars.BuildingBlocks.Messaging.Tracing;

/// <summary>
/// Extracts the W3C trace context carried by an incoming message and starts the consumer-side span
/// it becomes a child of (§6.3.2, D8). Registered by
/// <c>MessagingServiceCollectionExtensions.AddSbaCarsMessaging</c> to run <b>before</b>
/// <c>Rebus.Pipeline.Receive.DeserializeIncomingMessageStep</c>, so the span is already open by the
/// time deserialization runs — a message whose body fails to deserialize still gets a span (and gets
/// to report the failure on it) instead of the failure happening in a gap with no trace at all.
/// </summary>
/// <remarks>
/// Because this step runs before deserialization, the logical <c>Rebus.Messages.Message</c> is not
/// yet available on the context — only the raw <see cref="TransportMessage"/> is. The event's wire
/// name for the span name/tag therefore comes from the <see cref="CloudEventHeaders.Type"/>
/// (<c>ce_type</c>) header the publisher already stamped, rather than from re-resolving a .NET type
/// that has not been deserialized yet.
/// </remarks>
public sealed class TracingIncomingStep : IIncomingStep
{
    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        var transportMessage = context.Load<TransportMessage>();
        var headers = transportMessage.Headers;

        var topic = headers.GetValueOrDefault(CloudEventHeaders.Type, "unknown");
        headers.TryGetValue(Headers.MessageId, out var messageId);

        var extracted = Propagators.DefaultTextMapPropagator.Extract(
            default,
            headers,
            static (carrier, key) => carrier.TryGetValue(key, out var value) ? [value] : []);

        Baggage.Current = extracted.Baggage;

        using var activity = MessagingActivitySource.Instance.StartActivity(
            $"{topic} process",
            ActivityKind.Consumer,
            extracted.ActivityContext);

        if (activity is not null)
        {
            activity.SetTag("messaging.system", "rabbitmq");
            activity.SetTag("messaging.operation", "process");
            activity.SetTag("messaging.destination.name", topic);

            if (messageId is not null)
            {
                activity.SetTag("messaging.message.id", messageId);
            }
        }

        try
        {
            await next();
        }
        catch (Exception ex)
        {
            // The span reports the failure; it does not swallow it. Rebus' own retry strategy
            // (MessagingOptions.MaxDeliveryAttempts / second-level retries / error queue, §6.3) is
            // what decides whether this message is retried or dead-lettered — this step's only job
            // regarding the exception is to make it visible on the trace before it propagates.
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
