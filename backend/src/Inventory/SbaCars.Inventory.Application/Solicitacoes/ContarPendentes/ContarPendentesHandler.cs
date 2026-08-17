using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;

namespace SbaCars.Inventory.Application.Solicitacoes.ContarPendentes;

public sealed class ContarPendentesHandler(
    ISolicitacaoReadRepository readRepository,
    IClock clock) : IQueryHandler<ContarPendentesQuery, ContagemPendentesResponse>
{
    public Task<ContagemPendentesResponse> HandleAsync(
        ContarPendentesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return readRepository.ContarPendentesAsync(clock.UtcNow, cancellationToken);
    }
}
