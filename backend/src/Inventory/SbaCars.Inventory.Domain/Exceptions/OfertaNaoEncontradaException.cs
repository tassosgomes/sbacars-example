using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Exceptions;

public sealed class OfertaNaoEncontradaException(Guid ofertaId)
    : DomainException($"A oferta '{ofertaId}' não foi encontrada.")
{
    public Guid OfertaId { get; } = ofertaId;
}
