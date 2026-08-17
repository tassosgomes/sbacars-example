using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Exceptions;

public sealed class TipoSolicitacaoNaoPermitidoException(string? tipo)
    : DomainException("O tipo de solicitação não é suportado.")
{
    public string? TipoRecebido { get; } = tipo;
}
