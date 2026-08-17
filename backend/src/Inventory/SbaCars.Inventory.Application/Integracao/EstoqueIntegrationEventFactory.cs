using SbaCars.Contracts.Estoque.V1;

namespace SbaCars.Inventory.Application.Integracao;

/// <summary>
/// Creates the versioned inventory integration contracts used by the application use cases.
/// The factory keeps contract construction in one place while the transactional adapter remains
/// behind <see cref="IEstoqueIntegrationEventPublisher"/>.
/// </summary>
public sealed class EstoqueIntegrationEventFactory
{
    public OfertaIncluidaIntegrationEvent CreateOfferIncluded(
        Guid ofertaId,
        DateTimeOffset occurredAt) =>
        new(ofertaId, occurredAt);

    public OfertaAtualizadaIntegrationEvent CreateOfferUpdated(
        Guid ofertaId,
        DateTimeOffset occurredAt) =>
        new(ofertaId, occurredAt);

    public OfertaRetiradaIntegrationEvent CreateOfferWithdrawn(
        Guid ofertaId,
        DateTimeOffset occurredAt) =>
        new(ofertaId, occurredAt);

    public DisponibilidadeAlteradaIntegrationEvent CreateAvailabilityChanged(
        Guid ofertaId,
        string disponibilidade,
        DateTimeOffset occurredAt) =>
        new(ofertaId, disponibilidade, occurredAt);
}
