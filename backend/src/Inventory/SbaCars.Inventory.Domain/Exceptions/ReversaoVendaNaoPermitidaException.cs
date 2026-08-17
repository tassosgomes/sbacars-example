using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Exceptions;

public sealed class ReversaoVendaNaoPermitidaException()
    : DomainException("A reversão de venda só pode ser solicitada para uma oferta vendida.");
