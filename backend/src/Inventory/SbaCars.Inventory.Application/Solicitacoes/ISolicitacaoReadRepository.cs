using SbaCars.BuildingBlocks.Application;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Solicitacoes.ListarFilaValidacao;

namespace SbaCars.Inventory.Application.Solicitacoes;

public interface ISolicitacaoReadRepository
{
    Task<PagedResult<SolicitacaoResumoResponse>> ListarAsync(
        ListarFilaValidacaoQuery query,
        DateTimeOffset agora,
        CancellationToken cancellationToken);

    Task<ContagemPendentesResponse> ContarPendentesAsync(
        DateTimeOffset agora,
        CancellationToken cancellationToken);
}
