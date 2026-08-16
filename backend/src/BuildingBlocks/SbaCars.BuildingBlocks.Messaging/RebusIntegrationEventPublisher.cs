using Rebus.Bus;
using SbaCars.BuildingBlocks.Application;

namespace SbaCars.BuildingBlocks.Messaging;

/// <summary>
/// The Rebus-backed implementation of <see cref="IIntegrationEventPublisher"/> (§6) — registered by
/// <c>MessagingServiceCollectionExtensions.AddSbaCarsMessaging</c>, resolving <see cref="IBus"/> from
/// the same Rebus.ServiceProvider wiring that call configures.
/// </summary>
/// <remarks>
/// In B2, this class stages events on <see cref="IOutboxMessageStaging"/> when persistence is
/// registered; <see cref="SbaCars.BuildingBlocks.Persistence.EfUnitOfWork{TContext}"/> publishes
/// from inside the outbox's <see cref="Rebus.Transport.RebusTransactionScope"/> during
/// <c>SaveChangesAsync</c>, enlisting the publish in the same transaction as the EF Core
/// <c>SaveChanges</c> that produced the event.
/// </remarks>
public sealed class RebusIntegrationEventPublisher(IBus bus, IOutboxTransaction outboxTransaction)
    : IIntegrationEventPublisher
{
    public async Task PublishAsync(object integrationEvent, CancellationToken cancellationToken = default)
    {
        if (outboxTransaction is IOutboxMessageStaging staging)
        {
            staging.Stage(integrationEvent);
            return;
        }

        await outboxTransaction.EnsureOpenAsync(cancellationToken).ConfigureAwait(false);
        await bus.Publish(integrationEvent).ConfigureAwait(false);
    }
}
