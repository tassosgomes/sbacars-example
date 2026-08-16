using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Pipeline;
using SbaCars.Contracts.Foundation.V1;

namespace SbaCars.Catalog.Api.Messaging.Foundation;

/// <summary>
/// B5 scaffolding (§6.5): catalog consumer for <c>foundation.ping</c>. Lives in .Api — Rebus must
/// not enter Application/Domain (architecture tests). Delete when the first real catalog consumer exists.
/// </summary>
public sealed class FoundationPingHandler(FoundationPingReceipt receipt) : IHandleMessages<FoundationPingIntegrationEvent>
{
    public Task Handle(FoundationPingIntegrationEvent message)
    {
        var headers = MessageContext.Current.TransportMessage.Headers;
        receipt.Record(
            message.PingId,
            headers.GetValueOrDefault(Headers.MessageId),
            headers.GetValueOrDefault("traceparent"));
        return Task.CompletedTask;
    }
}
