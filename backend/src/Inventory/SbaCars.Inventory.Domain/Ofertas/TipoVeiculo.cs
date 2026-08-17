using SbaCars.Inventory.Domain.Exceptions;

namespace SbaCars.Inventory.Domain.Ofertas;

public enum TipoVeiculo
{
    CarroSeminovo,
    CarroUsado,
}

public static class TipoVeiculoExtensions
{
    public static string ToContractValue(this TipoVeiculo tipo) => tipo switch
    {
        TipoVeiculo.CarroSeminovo => "carro-seminovo",
        TipoVeiculo.CarroUsado => "carro-usado",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, null),
    };

    public static TipoVeiculo ParseTipoVeiculo(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "carro-seminovo" => TipoVeiculo.CarroSeminovo,
            "carro-usado" => TipoVeiculo.CarroUsado,
            _ => throw new TipoVeiculoNaoPermitidoException(value),
        };
    }
}
