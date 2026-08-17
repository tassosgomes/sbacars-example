using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Exceptions;

public sealed class OfertaJaRetiradaException()
    : DomainException("A oferta já está retirada.");
