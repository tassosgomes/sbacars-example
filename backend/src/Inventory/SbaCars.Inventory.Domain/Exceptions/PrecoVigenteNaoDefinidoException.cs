using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Exceptions;

public sealed class PrecoVigenteNaoDefinidoException()
    : DomainException("A alteração de preço exige um preço oficial vigente.");
