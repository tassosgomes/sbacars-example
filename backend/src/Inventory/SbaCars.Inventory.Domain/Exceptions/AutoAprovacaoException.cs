using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Exceptions;

/// <summary>Indicates that the requester attempted to approve their own request.</summary>
public sealed class AutoAprovacaoException()
    : DomainException("Quem abriu a solicitação não pode aprová-la.");
