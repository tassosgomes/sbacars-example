using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;

namespace SbaCars.Inventory.Application.Solicitacoes.RejeitarSolicitacao;

public sealed record RejeitarSolicitacaoCommand : ICommand<SolicitacaoDetalheResponse>
{
    public Guid SolicitacaoId { get; init; }

    public string? Justificativa { get; init; }
}
