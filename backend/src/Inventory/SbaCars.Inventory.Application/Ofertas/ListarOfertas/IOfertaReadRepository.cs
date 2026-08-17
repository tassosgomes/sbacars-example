using SbaCars.BuildingBlocks.Application;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Application.Ofertas.ListarOfertas;

public interface IOfertaReadRepository
{
    Task<PagedResult<OfertaResumoResponse>> ListarAsync(
        ListarOfertasQuery query,
        CancellationToken cancellationToken);

    Task<OfertaDetalheResponse?> ObterDetalheAsync(
        Guid ofertaId,
        CancellationToken cancellationToken);
}
