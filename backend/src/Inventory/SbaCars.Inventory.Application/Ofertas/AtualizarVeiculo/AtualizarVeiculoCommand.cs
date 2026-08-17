using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Application.Ofertas.AtualizarVeiculo;

public sealed record AtualizarVeiculoCommand : ICommand<OfertaDetalheResponse>
{
    public Guid OfertaId { get; init; }

    public bool TipoVeiculoInformado { get; init; }

    public string? TipoVeiculo { get; init; }

    public bool PlacaInformada { get; init; }

    public string? Placa { get; init; }

    public bool ChassiInformado { get; init; }

    public string? Chassi { get; init; }

    public bool MarcaInformada { get; init; }

    public string? Marca { get; init; }

    public bool ModeloInformado { get; init; }

    public string? Modelo { get; init; }

    public bool VersaoInformada { get; init; }

    public string? Versao { get; init; }

    public bool AnoFabricacaoInformado { get; init; }

    public int? AnoFabricacao { get; init; }

    public bool AnoModeloInformado { get; init; }

    public int? AnoModelo { get; init; }

    public bool QuilometragemInformada { get; init; }

    public int? Quilometragem { get; init; }

    public bool CorInformada { get; init; }

    public string? Cor { get; init; }

    public bool CombustivelInformado { get; init; }

    public string? Combustivel { get; init; }

    public bool CambioInformado { get; init; }

    public string? Cambio { get; init; }

    public bool LocalizacaoInformada { get; init; }

    public LocalizacaoPatch? Localizacao { get; init; }

    public bool ConfirmaSuspensao { get; init; }

    public bool TemAlteracoes => TipoVeiculoInformado ||
                                 PlacaInformada ||
                                 ChassiInformado ||
                                 MarcaInformada ||
                                 ModeloInformado ||
                                 VersaoInformada ||
                                 AnoFabricacaoInformado ||
                                 AnoModeloInformado ||
                                 QuilometragemInformada ||
                                 CorInformada ||
                                 CombustivelInformado ||
                                 CambioInformado ||
                                 LocalizacaoInformada;

    public VeiculoPatch ToDomainPatch() => new()
    {
        TipoVeiculoInformado = TipoVeiculoInformado,
        TipoVeiculo = TipoVeiculoInformado
            ? TipoVeiculoExtensions.ParseTipoVeiculo(TipoVeiculo)
            : null,
        PlacaInformada = PlacaInformada,
        Placa = Placa,
        ChassiInformado = ChassiInformado,
        Chassi = Chassi,
        MarcaInformada = MarcaInformada,
        Marca = Marca,
        ModeloInformado = ModeloInformado,
        Modelo = Modelo,
        VersaoInformada = VersaoInformada,
        Versao = Versao,
        AnoFabricacaoInformado = AnoFabricacaoInformado,
        AnoFabricacao = AnoFabricacao,
        AnoModeloInformado = AnoModeloInformado,
        AnoModelo = AnoModelo,
        QuilometragemInformada = QuilometragemInformada,
        Quilometragem = Quilometragem,
        CorInformada = CorInformada,
        Cor = Cor,
        CombustivelInformado = CombustivelInformado,
        Combustivel = Combustivel,
        CambioInformado = CambioInformado,
        Cambio = Cambio,
        LocalizacaoInformada = LocalizacaoInformada,
        Localizacao = Localizacao,
    };
}
