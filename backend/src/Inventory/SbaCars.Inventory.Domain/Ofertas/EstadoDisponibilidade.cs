using SbaCars.Inventory.Domain.Exceptions;

namespace SbaCars.Inventory.Domain.Ofertas;

public enum EstadoDisponibilidade
{
    Disponivel,
    Reservado,
    Vendido,
}

public static class EstadoDisponibilidadeExtensions
{
    public static string ToContractValue(this EstadoDisponibilidade estado) => estado switch
    {
        EstadoDisponibilidade.Disponivel => "disponivel",
        EstadoDisponibilidade.Reservado => "reservado",
        EstadoDisponibilidade.Vendido => "vendido",
        _ => throw new ArgumentOutOfRangeException(nameof(estado), estado, null),
    };

    public static EstadoDisponibilidade Parse(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "disponivel" => EstadoDisponibilidade.Disponivel,
        "reservado" => EstadoDisponibilidade.Reservado,
        "vendido" => EstadoDisponibilidade.Vendido,
        _ => throw new EstadoDisponibilidadeNaoPermitidoException(value),
    };
}
