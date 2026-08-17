using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Exceptions;

public sealed class OfertaJaElegivelException()
    : DomainException("A oferta já está elegível e não precisa de uma nova solicitação de elegibilidade.");
