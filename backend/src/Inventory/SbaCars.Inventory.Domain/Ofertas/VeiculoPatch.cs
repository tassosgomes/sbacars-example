namespace SbaCars.Inventory.Domain.Ofertas;

/// <summary>
/// Represents the fields supplied by the partial vehicle update. The explicit presence flags
/// distinguish an omitted field (keep the current value) from a JSON null (clear the value).
/// </summary>
public sealed record VeiculoPatch
{
    public bool TipoVeiculoInformado { get; init; }

    public TipoVeiculo? TipoVeiculo { get; init; }

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
}

/// <summary>Partial update for the nested vehicle location value object.</summary>
public sealed record LocalizacaoPatch
{
    public bool CepInformado { get; init; }

    public string? Cep { get; init; }

    public bool CidadeInformada { get; init; }

    public string? Cidade { get; init; }

    public bool UfInformada { get; init; }

    public string? Uf { get; init; }
}
