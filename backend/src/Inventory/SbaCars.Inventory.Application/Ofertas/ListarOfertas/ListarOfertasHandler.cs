using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;

namespace SbaCars.Inventory.Application.Ofertas.ListarOfertas;

public sealed class ListarOfertasHandler(IOfertaReadRepository readRepository)
    : IQueryHandler<ListarOfertasQuery, PagedResult<OfertaResumoResponse>>
{
    public Task<PagedResult<OfertaResumoResponse>> HandleAsync(
        ListarOfertasQuery query,
        CancellationToken cancellationToken) =>
        readRepository.ListarAsync(query, cancellationToken);
}
