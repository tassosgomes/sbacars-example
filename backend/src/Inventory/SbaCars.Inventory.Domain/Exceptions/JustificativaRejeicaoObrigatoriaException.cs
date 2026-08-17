using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Exceptions;

/// <summary>Indicates that a rejection did not include its required reason.</summary>
public sealed class JustificativaRejeicaoObrigatoriaException()
    : DomainException("A justificativa é obrigatória para rejeitar uma solicitação.");
