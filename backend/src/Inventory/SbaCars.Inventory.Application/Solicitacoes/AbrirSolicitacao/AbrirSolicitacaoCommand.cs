using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Domain.Solicitacoes;

namespace SbaCars.Inventory.Application.Solicitacoes.AbrirSolicitacao;

public sealed record AbrirSolicitacaoCommand : ICommand<SolicitacaoDetalheResponse>
{
    public Guid OfertaId { get; init; }

    public TipoSolicitacao Tipo { get; init; }

    public long? NovoPrecoCentavos { get; init; }

    public string? Justificativa { get; init; }
}
