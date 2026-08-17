using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Infrastructure.Projecoes;

/// <summary>
/// Flat read projection for the D01 feed. Only public offer data is selected; aggregate
/// navigation and internal authorship/validation fields never cross the read boundary.
/// </summary>
internal sealed class OfertaElegivelProjection
{
    public Guid OfertaId { get; init; }

    public string? Placa { get; init; }

    public string? Chassi { get; init; }

    public TipoVeiculo TipoVeiculo { get; init; }

    public string? Marca { get; init; }

    public string? Modelo { get; init; }

    public string? Versao { get; init; }

    public int? AnoFabricacao { get; init; }

    public int? AnoModelo { get; init; }

    public int? Quilometragem { get; init; }

    public string? Cor { get; init; }

    public string? Combustivel { get; init; }

    public string? Cambio { get; init; }

    public string? Cep { get; init; }

    public string? Cidade { get; init; }

    public string? Uf { get; init; }

    public BlocoFatoTipo OrigemTipo { get; init; }

    public bool OrigemIndisponivel { get; init; }

    public string? OrigemDescricao { get; init; }

    public string? OrigemFonte { get; init; }

    public string? OrigemLimitacao { get; init; }

    public BlocoFatoTipo CondicaoTipo { get; init; }

    public bool CondicaoIndisponivel { get; init; }

    public string? CondicaoDescricao { get; init; }

    public string? CondicaoFonte { get; init; }

    public string? CondicaoLimitacao { get; init; }

    public BlocoFatoTipo HistoricoTipo { get; init; }

    public bool HistoricoIndisponivel { get; init; }

    public string? HistoricoDescricao { get; init; }

    public string? HistoricoFonte { get; init; }

    public string? HistoricoLimitacao { get; init; }

    public long PrecoValorCentavos { get; init; }

    public string PrecoMoeda { get; init; } = "BRL";

    public EstadoDisponibilidade Disponibilidade { get; init; }

    public DateTimeOffset AtualizadoEm { get; init; }
}

internal static class OfertaElegivelProjectionMapper
{
    public static OfertaElegivelResponse ToResponse(OfertaElegivelProjection row) => new(
        row.OfertaId,
        new VeiculoElegivelResponse(
            row.Placa,
            row.Chassi,
            row.TipoVeiculo.ToContractValue(),
            row.Marca,
            row.Modelo,
            row.Versao,
            row.AnoFabricacao,
            row.AnoModelo,
            row.Quilometragem,
            row.Cor,
            row.Combustivel,
            row.Cambio,
            new LocalizacaoElegivelResponse(row.Cep, row.Cidade, row.Uf)),
        new FatosElegiveisResponse(
            ToFato(row.OrigemTipo, row.OrigemIndisponivel, row.OrigemDescricao, row.OrigemFonte, row.OrigemLimitacao),
            ToFato(row.CondicaoTipo, row.CondicaoIndisponivel, row.CondicaoDescricao, row.CondicaoFonte, row.CondicaoLimitacao),
            ToFato(row.HistoricoTipo, row.HistoricoIndisponivel, row.HistoricoDescricao, row.HistoricoFonte, row.HistoricoLimitacao)),
        new PrecoElegivelResponse(row.PrecoValorCentavos, row.PrecoMoeda),
        row.Disponibilidade.ToContractValue(),
        row.AtualizadoEm.ToUniversalTime());

    private static FatoElegivelResponse ToFato(
        BlocoFatoTipo tipo,
        bool indisponivel,
        string? descricao,
        string? fonte,
        string? limitacao)
    {
        var atendeTransparencia = !string.IsNullOrWhiteSpace(descricao) ||
                                  !string.IsNullOrWhiteSpace(fonte) ||
                                  !string.IsNullOrWhiteSpace(limitacao);

        return new FatoElegivelResponse(
            tipo.ToContractValue(),
            indisponivel,
            descricao,
            fonte,
            limitacao,
            atendeTransparencia);
    }
}
