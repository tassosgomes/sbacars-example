using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Exceptions;

/// <summary>
/// Indicates that an offer already has its first official price and must use the validated
/// price-change flow for any subsequent change.
/// </summary>
public sealed class PrecoJaDefinidoException : DomainException
{
    public PrecoJaDefinidoException()
        : base("O preço oficial já foi definido. Use uma solicitação de alteração de preço.")
    {
    }
}
