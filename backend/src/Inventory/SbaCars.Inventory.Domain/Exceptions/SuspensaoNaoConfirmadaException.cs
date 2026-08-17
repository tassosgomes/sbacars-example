using SbaCars.BuildingBlocks.Domain;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Domain.Exceptions;

/// <summary>
/// Indicates that a vehicle edit would suspend an eligible offer and the caller did not confirm
/// that consequence. The candidate change is deliberately not applied to the aggregate.
/// </summary>
public sealed class SuspensaoNaoConfirmadaException(
    IReadOnlyCollection<CodigoCriterio> criteriosAfetados)
    : DomainException("Confirme a suspensão para prosseguir.")
{
    public const string Codigo = "suspensao-nao-confirmada";

    public IReadOnlyCollection<CodigoCriterio> CriteriosAfetados { get; } =
        criteriosAfetados?.ToArray() ?? throw new ArgumentNullException(nameof(criteriosAfetados));
}
