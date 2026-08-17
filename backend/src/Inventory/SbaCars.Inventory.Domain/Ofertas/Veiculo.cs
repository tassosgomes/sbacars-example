namespace SbaCars.Inventory.Domain.Ofertas;

/// <summary>
/// Vehicle data held by the Oferta aggregate. Fields other than TipoVeiculo may be absent while
/// the offer is in preparation (RF-01/RN-10).
/// </summary>
public sealed class Veiculo
{
    private Veiculo()
    {
        Localizacao = Localizacao.Vazia();
    }

    public Veiculo(
        TipoVeiculo tipoVeiculo,
        string? placa = null,
        string? chassi = null,
        string? marca = null,
        string? modelo = null,
        string? versao = null,
        int? anoFabricacao = null,
        int? anoModelo = null,
        int? quilometragem = null,
        string? cor = null,
        string? combustivel = null,
        string? cambio = null,
        Localizacao? localizacao = null)
    {
        TipoVeiculo = tipoVeiculo;
        Placa = Normalize(placa)?.Replace("-", string.Empty, StringComparison.Ordinal);
        Chassi = Normalize(chassi)?.ToUpperInvariant();
        Marca = Normalize(marca);
        Modelo = Normalize(modelo);
        Versao = Normalize(versao);
        AnoFabricacao = anoFabricacao;
        AnoModelo = anoModelo;
        Quilometragem = quilometragem;
        Cor = Normalize(cor);
        Combustivel = Normalize(combustivel);
        Cambio = Normalize(cambio);
        Localizacao = localizacao ?? Localizacao.Vazia();
    }

    public TipoVeiculo TipoVeiculo { get; private set; }

    public string? Placa { get; private set; }

    public string? Chassi { get; private set; }

    public string? Marca { get; private set; }

    public string? Modelo { get; private set; }

    public string? Versao { get; private set; }

    public int? AnoFabricacao { get; private set; }

    public int? AnoModelo { get; private set; }

    public int? Quilometragem { get; private set; }

    public string? Cor { get; private set; }

    public string? Combustivel { get; private set; }

    public string? Cambio { get; private set; }

    public Localizacao Localizacao { get; private set; }

    internal Veiculo ComAlteracao(VeiculoPatch patch)
    {
        ArgumentNullException.ThrowIfNull(patch);

        return new Veiculo(
            patch.TipoVeiculoInformado ? patch.TipoVeiculo ?? TipoVeiculo : TipoVeiculo,
            patch.PlacaInformada ? patch.Placa : Placa,
            patch.ChassiInformado ? patch.Chassi : Chassi,
            patch.MarcaInformada ? patch.Marca : Marca,
            patch.ModeloInformado ? patch.Modelo : Modelo,
            patch.VersaoInformada ? patch.Versao : Versao,
            patch.AnoFabricacaoInformado ? patch.AnoFabricacao : AnoFabricacao,
            patch.AnoModeloInformado ? patch.AnoModelo : AnoModelo,
            patch.QuilometragemInformada ? patch.Quilometragem : Quilometragem,
            patch.CorInformada ? patch.Cor : Cor,
            patch.CombustivelInformado ? patch.Combustivel : Combustivel,
            patch.CambioInformado ? patch.Cambio : Cambio,
            patch.LocalizacaoInformada ? Localizacao.ComAlteracao(patch.Localizacao) : Localizacao);
    }

    internal void Aplicar(VeiculoPatch patch)
    {
        var atualizado = ComAlteracao(patch);
        TipoVeiculo = atualizado.TipoVeiculo;
        Placa = atualizado.Placa;
        Chassi = atualizado.Chassi;
        Marca = atualizado.Marca;
        Modelo = atualizado.Modelo;
        Versao = atualizado.Versao;
        AnoFabricacao = atualizado.AnoFabricacao;
        AnoModelo = atualizado.AnoModelo;
        Quilometragem = atualizado.Quilometragem;
        Cor = atualizado.Cor;
        Combustivel = atualizado.Combustivel;
        Cambio = atualizado.Cambio;
        Localizacao.Aplicar(atualizado.Localizacao);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
