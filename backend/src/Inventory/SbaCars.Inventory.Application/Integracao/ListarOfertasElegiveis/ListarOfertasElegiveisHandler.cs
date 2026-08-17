using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;

namespace SbaCars.Inventory.Application.Integracao.ListarOfertasElegiveis;

public sealed class ListarOfertasElegiveisHandler(
    IOfertaElegivelReadRepository readRepository)
    : IQueryHandler<ListarOfertasElegiveisQuery, PagedResult<OfertaElegivelResponse>>
{
    public Task<PagedResult<OfertaElegivelResponse>> HandleAsync(
        ListarOfertasElegiveisQuery query,
        CancellationToken cancellationToken) =>
        readRepository.ListarAsync(query, cancellationToken);
}
