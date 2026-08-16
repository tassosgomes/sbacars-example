using Rebus.Bus;
using SbaCars.BuildingBlocks.Application;

namespace SbaCars.BuildingBlocks.Messaging;

/// <summary>
/// The Rebus-backed implementation of <see cref="IIntegrationEventPublisher"/> (§6) — registered by
/// <c>MessagingServiceCollectionExtensions.AddSbaCarsMessaging</c>, resolving <see cref="IBus"/> from
/// the same Rebus.ServiceProvider wiring that call configures.
/// </summary>
/// <remarks>
/// In B2, this exact class starts publishing from inside the outbox's <c>RebusTransactionScope</c>
/// instead of calling <see cref="IBus.Publish"/> directly — the outbox enlists the publish in the
/// same transaction as the EF Core <c>SaveChanges</c> that produced the event, so a use case never
/// observes "the event was published but the transaction that produced it rolled back" or vice
/// versa. No use case that depends on <see cref="IIntegrationEventPublisher"/> needs to change for
/// that to happen; only this class' body does.
/// </remarks>
public sealed class RebusIntegrationEventPublisher(IBus bus) : IIntegrationEventPublisher
{
    public Task PublishAsync(object integrationEvent, CancellationToken cancellationToken = default) =>
        bus.Publish(integrationEvent);
}
