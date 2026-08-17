using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Exceptions;

public sealed class PlacaImutavelException : DomainException
{
    public PlacaImutavelException()
        : base("A placa só pode ser alterada enquanto a oferta está em preparação.")
    {
    }
}
