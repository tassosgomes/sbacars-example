using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Exceptions;

/// <summary>Indicates that a validation request does not exist.</summary>
public sealed class SolicitacaoNaoEncontradaException(Guid solicitacaoId)
    : DomainException($"A solicitação '{solicitacaoId}' não foi encontrada.")
{
    public Guid SolicitacaoId { get; } = solicitacaoId;
}
