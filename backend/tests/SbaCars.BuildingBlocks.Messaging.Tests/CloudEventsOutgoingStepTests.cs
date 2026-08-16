using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Send;
using SbaCars.BuildingBlocks.Messaging.CloudEvents;

namespace SbaCars.BuildingBlocks.Messaging.Tests;

/// <summary>
/// Proves D7: <see cref="CloudEventsOutgoingStep"/> stamps the CloudEvents 1.0 binary-content-mode
/// envelope onto every outgoing <see cref="TransportMessage"/>.
/// </summary>
public sealed class CloudEventsOutgoingStepTests
{
    private const string ServiceName = "inventory-service";

    [Fact]
    public async Task StampsEveryCloudEventsHeader_WithTheValuesD7Specifies()
    {
        var transportMessage = await RunStepAsync(new ProbeEvent(), messageId: "rbs2-msg-id-value");

        transportMessage.Headers[CloudEventHeaders.SpecVersion].Should().Be("1.0");
        transportMessage.Headers[CloudEventHeaders.Type].Should().Be("test.probe");
        transportMessage.Headers[CloudEventHeaders.Source].Should().Be($"urn:sbacars:{ServiceName}");
        transportMessage.Headers[CloudEventHeaders.DataContentType].Should().Be("application/json");
    }

    /// <remarks>
    /// This is the assertion the whole test file exists for (D7): B3's inbox will deduplicate
    /// incoming messages by this exact <c>ce_id</c> value, so it must be identical to the
    /// <c>rbs2-msg-id</c> Rebus already generated — never a second, independently minted identifier.
    /// If a future change makes this a fresh <c>Guid</c> instead, B3's dedup silently stops working.
    /// </remarks>
    [Fact]
    public async Task CeId_IsExactlyTheRbs2MsgIdHeaderRebusAlreadyGenerated()
    {
        const string rebusMessageId = "11111111-2222-3333-4444-555555555555";

        var transportMessage = await RunStepAsync(new ProbeEvent(), messageId: rebusMessageId);

        transportMessage.Headers[CloudEventHeaders.Id].Should().Be(rebusMessageId);
    }

    [Fact]
    public async Task CeTime_IsParseableAsRfc3339_AndIsInUtc()
    {
        var transportMessage = await RunStepAsync(new ProbeEvent(), messageId: "some-id");

        var parsed = DateTimeOffset.Parse(
            transportMessage.Headers[CloudEventHeaders.Time],
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind);

        parsed.Offset.Should().Be(TimeSpan.Zero);
        parsed.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task AHeaderThatIsAlreadyPresent_IsNotOverwritten()
    {
        var headers = new Dictionary<string, string>();
        var transportMessage = new TransportMessage(headers, []);
        transportMessage.Headers[CloudEventHeaders.SpecVersion] = "already-set-by-someone-else";

        await RunStepAsync(new ProbeEvent(), messageId: "some-id", transportMessage);

        transportMessage.Headers[CloudEventHeaders.SpecVersion].Should().Be("already-set-by-someone-else");
    }

    [Fact]
    public async Task AnUnresolvableMessageType_DoesNotThrow_AndOmitsCeTypeInstead()
    {
        // IntegrationEventTopicConvention.GetTopic already fails the publish itself, with an
        // actionable message, before this step would ever run for real (see
        // MessagingServiceCollectionExtensions' pipeline ordering). This step's own job is only to
        // not make a bad situation worse with a second, less useful exception.
        var transportMessage = await RunStepAsync(new UnattributedEvent(), messageId: "some-id");

        transportMessage.Headers.Should().NotContainKey(CloudEventHeaders.Type);
    }

    private static async Task<TransportMessage> RunStepAsync(
        object messageBody, string messageId, TransportMessage? transportMessage = null)
    {
        var messageHeaders = new Dictionary<string, string> { [Headers.MessageId] = messageId };
        var message = new Message(messageHeaders, messageBody);

        // By the time this step runs (registered to run *after* SerializeOutgoingMessageStep — see
        // MessagingServiceCollectionExtensions), the TransportMessage's own header dictionary
        // already carries every Message header, including rbs2-msg-id — that copy is what
        // SerializeOutgoingMessageStep itself performs, so the test recreates it here instead of
        // starting the TransportMessage with an empty header set.
        transportMessage ??= new TransportMessage(new Dictionary<string, string>(messageHeaders), []);

        var context = new OutgoingStepContext(
            message, new FakeTransactionContext(), new DestinationAddresses(["dummy-queue"]));
        context.Save(transportMessage);

        var step = new CloudEventsOutgoingStep(ServiceName);
        await step.Process(context, () => Task.CompletedTask);

        return transportMessage;
    }
}
