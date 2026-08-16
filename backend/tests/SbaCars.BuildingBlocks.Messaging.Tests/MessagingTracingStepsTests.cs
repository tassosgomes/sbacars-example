using System.Diagnostics;
using System.Text.RegularExpressions;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Send;
using SbaCars.BuildingBlocks.Messaging.CloudEvents;
using SbaCars.BuildingBlocks.Messaging.Tracing;

namespace SbaCars.BuildingBlocks.Messaging.Tests;

/// <summary>
/// Proves D8: <see cref="TracingOutgoingStep"/>/<see cref="TracingIncomingStep"/> start their own
/// spans (rather than relying on <c>Rebus.OpenTelemetry</c> — see D2/the remarks on
/// <c>CloudEventsOutgoingStep</c> for why that package was rejected) and propagate W3C trace context
/// over the transport message's own header dictionary.
/// </summary>
/// <remarks>
/// Builds a real <see cref="TracerProvider"/> with an in-memory exporter per test — the same idiom
/// <c>SbaCars.Gateway.Tests.TraceContinuityTests</c> uses — rather than only registering a raw
/// <see cref="ActivityListener"/>. This is not just about capturing spans: building a
/// <see cref="TracerProvider"/> is also what makes
/// <c>OpenTelemetry.Context.Propagation.Propagators.DefaultTextMapPropagator</c> a real, working W3C
/// propagator instead of its own default (a no-op) — in the real host that happens as a side effect
/// of <c>AddSbaCarsObservability</c> (A8) building the process' <c>TracerProvider</c>; these steps
/// never build one of their own (see <c>BuildingBlocks.Messaging.csproj</c>'s own comment on
/// <c>OpenTelemetry.Api</c>-only), so a test exercising them in isolation has to stand in for that
/// side effect exactly like this.
/// </remarks>
public sealed class MessagingTracingStepsTests
{
    private static readonly Regex TraceparentPattern = new(
        "^00-(?<traceId>[0-9a-f]{32})-(?<spanId>[0-9a-f]{16})-(?<flags>0[01])$", RegexOptions.Compiled);

    [Fact]
    public async Task OutgoingStep_InjectsAValidW3CTraceparentHeader_MatchingTheActivityItStarted()
    {
        using var tracerProvider = BuildTracerProvider(out var exportedActivities);

        var transportMessage = await RunOutgoingStepAsync(new ProbeEvent());

        var startedActivity = exportedActivities.Should().ContainSingle().Subject;
        var traceparent = transportMessage.Headers["traceparent"];

        var match = TraceparentPattern.Match(traceparent);
        match.Success.Should().BeTrue($"'{traceparent}' must be a well-formed W3C traceparent header");
        match.Groups["traceId"].Value.Should().Be(startedActivity.TraceId.ToHexString());
        match.Groups["spanId"].Value.Should().Be(startedActivity.SpanId.ToHexString());
    }

    /// <remarks>
    /// This is the exact case D2 says <c>Rebus.OpenTelemetry</c>'s own outgoing step gives up on
    /// (<c>StartActivity</c> returning <see langword="null"/> when <c>Activity.Current</c> is
    /// <see langword="null"/>) — a publish from a background service or the B2 outbox forwarder,
    /// neither of which runs inside an inbound HTTP request's activity. This is the reason
    /// <c>TracingOutgoingStep</c> exists as its own code instead of reusing that package: it must
    /// still produce a (root) span and a valid <c>traceparent</c> here, not silently vanish.
    /// </remarks>
    [Fact]
    public async Task OutgoingStep_WithNoAmbientActivityCurrent_StillProducesARootSpanAndATraceparent()
    {
        Activity.Current = null;
        using var tracerProvider = BuildTracerProvider(out var exportedActivities);

        var transportMessage = await RunOutgoingStepAsync(new ProbeEvent());

        var startedActivity = exportedActivities.Should().ContainSingle().Subject;
        startedActivity.ParentSpanId.Should().Be(default(ActivitySpanId), "with no ambient Activity.Current the span must be a root span, not parentless-and-missing");
        TraceparentPattern.IsMatch(transportMessage.Headers["traceparent"]).Should().BeTrue();
    }

    [Fact]
    public async Task IncomingStep_GivenASyntheticTraceparent_StartsAConsumerActivity_OnTheSameTraceId_WithTheCorrectParent()
    {
        var injectedTraceId = ActivityTraceId.CreateRandom();
        var injectedParentSpanId = ActivitySpanId.CreateRandom();
        var headers = new Dictionary<string, string>
        {
            ["traceparent"] = $"00-{injectedTraceId.ToHexString()}-{injectedParentSpanId.ToHexString()}-01",
            [CloudEventHeaders.Type] = "test.probe",
            [Headers.MessageId] = "some-id",
        };

        using var tracerProvider = BuildTracerProvider(out var exportedActivities);

        await RunIncomingStepAsync(headers, next: () => Task.CompletedTask);

        var startedActivity = exportedActivities.Should().ContainSingle().Subject;
        startedActivity.Kind.Should().Be(ActivityKind.Consumer);
        startedActivity.TraceId.Should().Be(injectedTraceId);
        startedActivity.ParentSpanId.Should().Be(injectedParentSpanId);
    }

    [Fact]
    public async Task IncomingStep_WhenTheHandlerThrows_MarksTheActivityAsError_AndRethrows()
    {
        var headers = new Dictionary<string, string>
        {
            [CloudEventHeaders.Type] = "test.probe",
            [Headers.MessageId] = "some-id",
        };

        using var tracerProvider = BuildTracerProvider(out var exportedActivities);

        var thrown = new InvalidOperationException("handler exploded");
        var act = () => RunIncomingStepAsync(headers, next: () => throw thrown);

        // The retry decision (first-level retry / second-level / error queue, §6.3) belongs to
        // Rebus, not to this step — the step's only job regarding the exception is to make it
        // visible on the span before letting it propagate.
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("handler exploded");

        var startedActivity = exportedActivities.Should().ContainSingle().Subject;
        startedActivity.Status.Should().Be(ActivityStatusCode.Error);
    }

    private static async Task<TransportMessage> RunOutgoingStepAsync(object messageBody)
    {
        var messageHeaders = new Dictionary<string, string> { [Headers.MessageId] = "some-id" };
        var message = new Message(messageHeaders, messageBody);
        // See CloudEventsOutgoingStepTests' RunStepAsync remarks: by the time this step runs, the
        // TransportMessage's headers already carry every Message header (SerializeOutgoingMessageStep
        // runs first), including rbs2-msg-id — reproduced here so messaging.message.id is populated
        // exactly as it would be for real.
        var transportMessage = new TransportMessage(new Dictionary<string, string>(messageHeaders), []);

        var context = new OutgoingStepContext(
            message, new FakeTransactionContext(), new DestinationAddresses(["dummy-queue"]));
        context.Save(transportMessage);

        var step = new TracingOutgoingStep();
        await step.Process(context, () => Task.CompletedTask);

        return transportMessage;
    }

    private static async Task RunIncomingStepAsync(Dictionary<string, string> headers, Func<Task> next)
    {
        var transportMessage = new TransportMessage(headers, []);
        var context = new IncomingStepContext(transportMessage, new FakeTransactionContext());

        var step = new TracingIncomingStep();
        await step.Process(context, next);
    }

    private static TracerProvider BuildTracerProvider(out List<Activity> exportedActivities)
    {
        var activities = new List<Activity>();
        exportedActivities = activities;

        return Sdk.CreateTracerProviderBuilder()
            .AddSource(MessagingActivitySource.Name)
            .AddInMemoryExporter(activities)
            .Build();
    }
}
