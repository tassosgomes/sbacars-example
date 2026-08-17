using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;

namespace SbaCars.Inventory.Application.Solicitacoes.AprovarSolicitacao;

public sealed record AprovarSolicitacaoCommand : ICommand<SolicitacaoDetalheResponse>
{
    public Guid SolicitacaoId { get; init; }

    public string? Observacao { get; init; }
}
