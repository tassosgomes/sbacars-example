using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Ofertas.ListarOfertas;
using SbaCars.Inventory.Domain.Exceptions;

namespace SbaCars.Inventory.Application.Ofertas.ObterOferta;

public sealed class ObterOfertaHandler(IOfertaReadRepository readRepository)
    : IQueryHandler<ObterOfertaQuery, OfertaDetalheResponse>
{
    public async Task<OfertaDetalheResponse> HandleAsync(
        ObterOfertaQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var response = await readRepository
            .ObterDetalheAsync(query.OfertaId, cancellationToken)
            .ConfigureAwait(false);

        return response ?? throw new OfertaNaoEncontradaException(query.OfertaId);
    }
}
