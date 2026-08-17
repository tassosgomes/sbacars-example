using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Exceptions;

/// <summary>Indicates that a request already has a final decision.</summary>
public sealed class SolicitacaoJaDecididaException(Guid solicitacaoId)
    : DomainException("A solicitação já foi decidida.")
{
    public Guid SolicitacaoId { get; } = solicitacaoId;
}
