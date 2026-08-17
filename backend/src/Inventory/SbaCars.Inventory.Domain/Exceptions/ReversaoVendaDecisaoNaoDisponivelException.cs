using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Exceptions;

/// <summary>
/// Keeps reversal approval behind the availability transition model delivered in V-08.
/// </summary>
public sealed class ReversaoVendaDecisaoNaoDisponivelException()
    : DomainException("A decisão de reversão de venda será habilitada com a máquina de disponibilidade.");
