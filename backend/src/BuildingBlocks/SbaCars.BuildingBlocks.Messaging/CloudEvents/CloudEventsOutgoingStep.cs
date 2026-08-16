using Rebus.Messages;
using Rebus.Pipeline;
using SbaCars.BuildingBlocks.Messaging.Topology;

namespace SbaCars.BuildingBlocks.Messaging.CloudEvents;

/// <summary>
/// Stamps the CloudEvents 1.0 binary-content-mode envelope (§6.3.2, D7) onto every outgoing
/// <see cref="TransportMessage"/>. Registered by <c>MessagingServiceCollectionExtensions.AddSbaCarsMessaging</c>
/// to run <b>after</b> <c>Rebus.Pipeline.Send.SerializeOutgoingMessageStep</c> — that is the point in
/// the send pipeline where the <see cref="TransportMessage"/> (and its header dictionary) actually
/// exists to be stamped; before that point, only the logical <c>Rebus.Messages.Message</c> is
/// available.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why not <c>Rebus.OpenTelemetry</c> (this step's spans live in <c>Tracing/</c>, but the same
/// package was evaluated once for both concerns and rejected for both — recorded here per §6.3.2's
/// instruction to write this down where someone would look for it).</b> Rebus.OpenTelemetry 1.4.0
/// (+ Rebus.Diagnostics 1.4.0), from rebus-org itself, exists and is maintained, but was read and
/// rejected for three concrete reasons:
/// </para>
/// <list type="number">
/// <item>It does not propagate the W3C <c>traceparent</c> header at all. It injects
/// <c>Activity.Id</c> into a proprietary <c>rbs-ot-tracestate</c> header and baggage into a
/// proprietary <c>rbs-ot-correlation-context</c> JSON header. §6.3.2 requires a "CloudEvents envelope
/// with <c>traceparent</c> propagated" — a proprietary header breaks correlation for any consumer
/// that is not this exact codebase.</item>
/// <item>Its outgoing step gives up on the span entirely when <c>Activity.Current == null</c>
/// (<c>StartActivity</c> returns <see langword="null"/> with no ambient parent). A publish issued
/// from the outbox forwarder (B2) or a background <c>IHostedService</c> (B7) — neither of which runs
/// inside an inbound HTTP request's Activity — would run with no trace at all, silently. §6.3.2 treats
/// end-to-end correlation as a requirement, not something that is allowed to quietly disappear
/// depending on the caller.</item>
/// <item>It targets <c>netstandard2.0</c>, is pinned to Rebus 8.0.1 and OpenTelemetry 1.6.0, and does
/// not touch the CloudEvents envelope at all — this outgoing step would have to exist regardless of
/// whether that package were referenced.</item>
/// </list>
/// <para>
/// The chosen alternative — own outgoing/incoming steps, described in <c>Tracing/</c> — starts a span
/// even with no ambient parent (a root span, not a lost one) and propagates trace context with the
/// standard W3C <see cref="OpenTelemetry.Context.Propagation.Propagators.DefaultTextMapPropagator"/>
/// over the very same header dictionary this step writes CloudEvents attributes into.
/// </para>
/// </remarks>
public sealed class CloudEventsOutgoingStep(string serviceName) : IOutgoingStep
{
    private readonly string _source = $"urn:sbacars:{serviceName}";

    public async Task Process(OutgoingStepContext context, Func<Task> next)
    {
        var transportMessage = context.Load<TransportMessage>();
        var headers = transportMessage.Headers;

        // Idempotent: a step that runs twice on the same message (or a message that was already
        // stamped upstream, e.g. forwarded as-is) must never clobber an attribute that already made
        // sense the first time it was set.
        headers.TryAdd(CloudEventHeaders.SpecVersion, "1.0");

        // The message type may not be resolvable to a wire name (no [IntegrationEvent] attribute).
        // That is not this step's problem to raise: IntegrationEventTopicConvention already fails the
        // publish earlier, with an actionable message pointing at the missing attribute. This step's
        // job is only to not make things worse by throwing a second, less useful exception — it
        // simply omits ce_type in that case.
        var messageType = context.Load<Message>()?.Body?.GetType();
        if (messageType is not null && IntegrationEventTopicConvention.TryGetTopic(messageType, out var topic))
        {
            headers.TryAdd(CloudEventHeaders.Type, topic);
        }

        // ce_id deliberately reuses the same rbs2-msg-id Rebus already generated for this message,
        // rather than minting a second identifier — this is not a detail. B3's inbox will deduplicate
        // incoming messages by this exact same id, so publisher and consumer need to agree on one
        // identifier for "this message", not two that happen to travel together.
        if (headers.TryGetValue(Headers.MessageId, out var messageId))
        {
            headers.TryAdd(CloudEventHeaders.Id, messageId);
        }

        headers.TryAdd(CloudEventHeaders.Source, _source);
        headers.TryAdd(CloudEventHeaders.Time, DateTimeOffset.UtcNow.ToString("O"));
        headers.TryAdd(CloudEventHeaders.DataContentType, "application/json");

        await next();
    }
}
