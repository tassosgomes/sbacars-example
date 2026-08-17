using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Exceptions;

public sealed class TipoVeiculoNaoPermitidoException(string? tipo)
    : DomainException("Somente carros seminovos ou usados compõem o estoque curado.")
{
    public string? TipoRecebido { get; } = tipo;
}
