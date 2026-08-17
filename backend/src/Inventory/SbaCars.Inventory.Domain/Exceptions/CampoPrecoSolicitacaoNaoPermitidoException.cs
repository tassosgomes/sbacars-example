using SbaCars.BuildingBlocks.Domain;
using SbaCars.Inventory.Domain.Solicitacoes;

namespace SbaCars.Inventory.Domain.Exceptions;

public sealed class CampoPrecoSolicitacaoNaoPermitidoException(TipoSolicitacao tipo)
    : DomainException("novoPrecoCentavos só pode ser informado em uma solicitação de preço.")
{
    public TipoSolicitacao Tipo { get; } = tipo;
}
