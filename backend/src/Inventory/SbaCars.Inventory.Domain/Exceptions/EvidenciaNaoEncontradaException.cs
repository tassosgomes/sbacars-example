using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Exceptions;

public sealed class EvidenciaNaoEncontradaException(Guid evidenciaId)
    : DomainException($"A evidência '{evidenciaId}' não foi encontrada.")
{
    public Guid EvidenciaId { get; } = evidenciaId;
}
