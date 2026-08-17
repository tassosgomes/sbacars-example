using SbaCars.BuildingBlocks.Domain;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Domain.Exceptions;

/// <summary>
/// Indicates that a fact was marked as unavailable without telling the buyer what is not known.
/// </summary>
public sealed class LimitacaoNaoDeclaradaException(BlocoFatoTipo tipo)
    : DomainException("Uma limitação deve ser declarada quando o fato está indisponível.")
{
    public BlocoFatoTipo Tipo { get; } = tipo;
}
