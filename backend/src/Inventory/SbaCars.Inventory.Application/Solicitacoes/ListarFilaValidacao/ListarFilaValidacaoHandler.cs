using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;

namespace SbaCars.Inventory.Application.Solicitacoes.ListarFilaValidacao;

public sealed class ListarFilaValidacaoHandler(
    ISolicitacaoReadRepository readRepository,
    IClock clock) : IQueryHandler<ListarFilaValidacaoQuery, PagedResult<SolicitacaoResumoResponse>>
{
    public Task<PagedResult<SolicitacaoResumoResponse>> HandleAsync(
        ListarFilaValidacaoQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return readRepository.ListarAsync(query, clock.UtcNow, cancellationToken);
    }
}
