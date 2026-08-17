using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;

namespace SbaCars.Inventory.Application.Solicitacoes.ObterSolicitacao;

public sealed record ObterSolicitacaoQuery(Guid SolicitacaoId)
    : IQuery<SolicitacaoDetalheResponse>;
