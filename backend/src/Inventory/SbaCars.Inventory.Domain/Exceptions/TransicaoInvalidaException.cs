using SbaCars.BuildingBlocks.Domain;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Domain.Exceptions;

/// <summary>Indicates that a direct availability transition is not allowed.</summary>
public sealed class TransicaoInvalidaException(
    EstadoDisponibilidade estadoAtual,
    EstadoDisponibilidade novoEstado)
    : DomainException("A transição de disponibilidade não é permitida.")
{
    public EstadoDisponibilidade EstadoAtual { get; } = estadoAtual;

    public EstadoDisponibilidade NovoEstado { get; } = novoEstado;
}
