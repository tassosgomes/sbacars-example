using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Exceptions;

public sealed class PlacaDuplicadaException(string placa)
    : DomainException($"Já existe uma oferta ativa para a placa '{placa}'.")
{
    public string Placa { get; } = placa;
}
