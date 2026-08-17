using SbaCars.BuildingBlocks.Domain;
using SbaCars.Inventory.Domain.Solicitacoes;

namespace SbaCars.Inventory.Domain.Exceptions;

public sealed class SolicitacaoPendenteDuplicadaException(Guid ofertaId, TipoSolicitacao tipo)
    : DomainException("Já existe uma solicitação pendente deste tipo para a oferta.")
{
    public Guid OfertaId { get; } = ofertaId;

    public TipoSolicitacao Tipo { get; } = tipo;
}
