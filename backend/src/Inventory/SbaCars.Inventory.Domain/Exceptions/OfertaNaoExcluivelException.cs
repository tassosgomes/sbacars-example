using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Exceptions;

public sealed class OfertaNaoExcluivelException : DomainException
{
    public OfertaNaoExcluivelException()
        : base("A oferta só pode ser excluída enquanto está em preparação.")
    {
    }
}
