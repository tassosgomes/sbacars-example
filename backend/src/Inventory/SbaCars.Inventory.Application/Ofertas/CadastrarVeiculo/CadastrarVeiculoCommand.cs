using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;

namespace SbaCars.Inventory.Application.Ofertas.CadastrarVeiculo;

public sealed record LocalizacaoInput(string? Cep, string? Cidade, string? Uf);

public sealed record CadastrarVeiculoCommand : ICommand<OfertaDetalheResponse>
{
    public string? Placa { get; init; }

    public string? Chassi { get; init; }

    public string? TipoVeiculo { get; init; }

    public string? Marca { get; init; }

    public string? Modelo { get; init; }

    public string? Versao { get; init; }

    public int? AnoFabricacao { get; init; }

    public int? AnoModelo { get; init; }

    public int? Quilometragem { get; init; }

    public string? Cor { get; init; }

    public string? Combustivel { get; init; }

    public string? Cambio { get; init; }

    public LocalizacaoInput? Localizacao { get; init; }
}
