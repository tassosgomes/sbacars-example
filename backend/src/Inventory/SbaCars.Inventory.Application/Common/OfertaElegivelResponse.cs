namespace SbaCars.Inventory.Application.Common;

/// <summary>
/// Public D01 feed projection. It deliberately contains no checklist, pending request,
/// validation-flow or internal authorship fields.
/// </summary>
public sealed record OfertaElegivelResponse(
    Guid OfertaId,
    VeiculoElegivelResponse Veiculo,
    FatosElegiveisResponse Fatos,
    PrecoElegivelResponse PrecoOficial,
    string Disponibilidade,
    DateTimeOffset AtualizadoEm);

public sealed record VeiculoElegivelResponse(
    string? Placa,
    string? Chassi,
    string TipoVeiculo,
    string? Marca,
    string? Modelo,
    string? Versao,
    int? AnoFabricacao,
    int? AnoModelo,
    int? Quilometragem,
    string? Cor,
    string? Combustivel,
    string? Cambio,
    LocalizacaoElegivelResponse Localizacao);

public sealed record LocalizacaoElegivelResponse(
    string? Cep,
    string? Cidade,
    string? Uf);

public sealed record FatosElegiveisResponse(
    FatoElegivelResponse Origem,
    FatoElegivelResponse Condicao,
    FatoElegivelResponse Historico);

public sealed record FatoElegivelResponse(
    string Tipo,
    bool Indisponivel,
    string? Descricao,
    string? Fonte,
    string? LimitacaoDeclarada,
    bool AtendeTransparencia);

public sealed record PrecoElegivelResponse(
    long ValorCentavos,
    string Moeda);
