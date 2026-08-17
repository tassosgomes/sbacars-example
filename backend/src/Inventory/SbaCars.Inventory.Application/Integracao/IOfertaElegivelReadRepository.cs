using SbaCars.BuildingBlocks.Application;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Integracao.ListarOfertasElegiveis;

namespace SbaCars.Inventory.Application.Integracao;

public interface IOfertaElegivelReadRepository
{
    Task<PagedResult<OfertaElegivelResponse>> ListarAsync(
        ListarOfertasElegiveisQuery query,
        CancellationToken cancellationToken);
}
