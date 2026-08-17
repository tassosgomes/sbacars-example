using SbaCars.BuildingBlocks.Domain;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Domain.Exceptions;

public sealed class CriteriosMinimosNaoAtendidosException(
    IReadOnlyCollection<CodigoCriterio> criterios)
    : DomainException("Todos os critérios mínimos devem ser atendidos para solicitar elegibilidade.")
{
    public IReadOnlyCollection<CodigoCriterio> Criterios { get; } =
        criterios?.ToArray() ?? throw new ArgumentNullException(nameof(criterios));
}
