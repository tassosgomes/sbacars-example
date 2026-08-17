using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Exceptions;

public sealed class EstadoDisponibilidadeNaoPermitidoException(string? estado)
    : DomainException("O estado de disponibilidade não é suportado.")
{
    public string? EstadoRecebido { get; } = estado;
}
