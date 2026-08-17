using System.Reflection;
using SbaCars.BuildingBlocks.Application;
using SbaCars.Contracts;
using SbaCars.Contracts.Estoque.V1;
using SbaCars.Inventory.Application.Common;

namespace SbaCars.Inventory.Application.Integracao;

/// <summary>
/// Application-facing inventory event port. Its adapter delegates to the foundation publisher,
/// which stages the message in the current transactional outbox when persistence is registered.
/// </summary>
public interface IEstoqueIntegrationEventPublisher
{
    Task PublishOfferIncludedAsync(
        Guid ofertaId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default);

    Task PublishOfferUpdatedAsync(
        Guid ofertaId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default);

    Task PublishOfferWithdrawnAsync(
        Guid ofertaId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default);

    Task PublishAvailabilityChangedAsync(
        Guid ofertaId,
        string disponibilidade,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Builds the existing V1 contracts and hands them to <see cref="IIntegrationEventPublisher"/>.
/// No RabbitMQ or Rebus API is referenced by a use-case handler; the foundation publisher stages
/// the record and <c>EfUnitOfWork</c> enlists it in the same transaction as the aggregate.
/// </summary>
public sealed class EstoqueIntegrationEventPublisher(
    IIntegrationEventPublisher publisher,
    EstoqueIntegrationEventFactory factory) : IEstoqueIntegrationEventPublisher
{
    public Task PublishOfferIncludedAsync(
        Guid ofertaId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default) =>
        PublishAsync(
            factory.CreateOfferIncluded(ofertaId, occurredAt),
            cancellationToken);

    public Task PublishOfferUpdatedAsync(
        Guid ofertaId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default) =>
        PublishAsync(
            factory.CreateOfferUpdated(ofertaId, occurredAt),
            cancellationToken);

    public Task PublishOfferWithdrawnAsync(
        Guid ofertaId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default) =>
        PublishAsync(
            factory.CreateOfferWithdrawn(ofertaId, occurredAt),
            cancellationToken);

    public Task PublishAvailabilityChangedAsync(
        Guid ofertaId,
        string disponibilidade,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default) =>
        PublishAsync(
            factory.CreateAvailabilityChanged(ofertaId, disponibilidade, occurredAt),
            cancellationToken);

    private async Task PublishAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        await publisher.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);

        var eventType = integrationEvent.GetType()
            .GetCustomAttribute<IntegrationEventAttribute>(inherit: false)
            ?.Name
            ?? throw new InvalidOperationException(
                $"Integration event '{integrationEvent.GetType().Name}' is missing its wire name.");

        InventoryMeters.EventPublished.Add(
            1,
            new KeyValuePair<string, object?>("tipo", eventType));
    }
}
