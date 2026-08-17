using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Application.Common;

public sealed record AutoriaResponse(string UsuarioId, string Nome, DateTimeOffset Em);

public sealed record LocalizacaoResponse(string? Cep, string? Cidade, string? Uf);

public sealed record VeiculoResponse(
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
    LocalizacaoResponse Localizacao);

public sealed record FatoResponse(
    string Tipo,
    bool Indisponivel,
    string? Descricao,
    string? Fonte,
    EvidenciaResponse? Evidencia,
    string? LimitacaoDeclarada,
    bool AtendeTransparencia,
    AutoriaResponse? AtualizadoPor);

public sealed record EvidenciaResponse(
    Guid EvidenciaId,
    string NomeArquivo,
    string TipoConteudo,
    long TamanhoBytes,
    DateTimeOffset EnviadaEm);

public sealed record FatosResponse(
    FatoResponse Origem,
    FatoResponse Condicao,
    FatoResponse Historico);

public sealed record PrecoOficialResponse(
    long ValorCentavos,
    string Moeda,
    AutoriaResponse? DefinidoPor);

public sealed record DisponibilidadeResponse(
    string Estado,
    DateTimeOffset Desde,
    AutoriaResponse? AlteradaPor,
    IReadOnlyCollection<string> TransicoesPermitidas);

public sealed record CriterioElegibilidadeResponse(
    string Codigo,
    bool Atendido,
    string? Pendencia);

public sealed record ChecklistElegibilidadeResponse(
    int Atendidos,
    int Total,
    IReadOnlyCollection<CriterioElegibilidadeResponse> Criterios,
    bool PodeSolicitarElegibilidade);

public sealed record PendenciaResumoResponse(
    Guid SolicitacaoId,
    string Tipo,
    string? ResumoAlteracao,
    DateTimeOffset AbertaEm,
    AutoriaResponse AbertaPor);

public sealed record OfertaResumoResponse(
    Guid OfertaId,
    string? Placa,
    string? DescricaoVeiculo,
    int? AnoFabricacao,
    int? AnoModelo,
    int? Quilometragem,
    LocalizacaoResponse Localizacao,
    long? PrecoOficialCentavos,
    string Situacao,
    string Disponibilidade,
    IReadOnlyCollection<string> Pendencias,
    DateTimeOffset AtualizadoEm);

public sealed record OfertaDetalheResponse(
    Guid OfertaId,
    string Situacao,
    string? MotivoSuspensao,
    DateTimeOffset? SuspensaEm,
    VeiculoResponse Veiculo,
    FatosResponse Fatos,
    PrecoOficialResponse? PrecoOficial,
    DisponibilidadeResponse Disponibilidade,
    ChecklistElegibilidadeResponse Elegibilidade,
    IReadOnlyCollection<PendenciaResumoResponse> Pendencias,
    DateTimeOffset CriadaEm,
    DateTimeOffset AtualizadoEm);

public static class OfertaResponseMapper
{
    private static readonly CodigoCriterio[] AllCriteria =
    [
        CodigoCriterio.Identificacao,
        CodigoCriterio.DadosBasicos,
        CodigoCriterio.Localizacao,
        CodigoCriterio.PrecoOficial,
        CodigoCriterio.Disponibilidade,
        CodigoCriterio.TransparenciaFatos,
    ];

    public static OfertaDetalheResponse ToDetalhe(
        Oferta oferta,
        IReadOnlyDictionary<Guid, Evidencia>? evidencias = null)
    {
        ArgumentNullException.ThrowIfNull(oferta);

        var faltantes = oferta.AvaliarCriteriosMinimos().ToHashSet();
        var criterios = AllCriteria
            .Select(codigo => new CriterioElegibilidadeResponse(
                codigo.ToContractValue(),
                !faltantes.Contains(codigo),
                faltantes.Contains(codigo) ? PendingText(codigo) : null))
            .ToArray();

        return new OfertaDetalheResponse(
            oferta.Id,
            oferta.Situacao.ToContractValue(),
            oferta.MotivoSuspensao,
            oferta.SuspensaEm,
            ToVeiculo(oferta.Veiculo),
            ToFatos(oferta.Fatos, evidencias),
            oferta.PrecoOficial is null ? null : new PrecoOficialResponse(
                oferta.PrecoOficial.ValorCentavos,
                oferta.PrecoOficial.Moeda,
                ToAutoria(oferta.PrecoOficial.DefinidoPor)),
            ToDisponibilidade(oferta.Disponibilidade),
            new ChecklistElegibilidadeResponse(
                AllCriteria.Length - faltantes.Count,
                criterios.Length,
                criterios,
                faltantes.Count == 0),
            [],
            oferta.CriadaEm,
            oferta.AtualizadoEm);
    }

    public static OfertaResumoResponse ToResumo(Oferta oferta)
    {
        ArgumentNullException.ThrowIfNull(oferta);

        return new OfertaResumoResponse(
            oferta.Id,
            oferta.Veiculo.Placa,
            JoinDescription(oferta.Veiculo),
            oferta.Veiculo.AnoFabricacao,
            oferta.Veiculo.AnoModelo,
            oferta.Veiculo.Quilometragem,
            ToLocalizacao(oferta.Veiculo.Localizacao),
            oferta.PrecoOficial?.ValorCentavos,
            oferta.Situacao.ToContractValue(),
            oferta.Disponibilidade.Estado.ToContractValue(),
            [],
            oferta.AtualizadoEm);
    }

    public static VeiculoResponse ToVeiculo(Veiculo veiculo) => new(
        veiculo.Placa,
        veiculo.Chassi,
        veiculo.TipoVeiculo.ToContractValue(),
        veiculo.Marca,
        veiculo.Modelo,
        veiculo.Versao,
        veiculo.AnoFabricacao,
        veiculo.AnoModelo,
        veiculo.Quilometragem,
        veiculo.Cor,
        veiculo.Combustivel,
        veiculo.Cambio,
        ToLocalizacao(veiculo.Localizacao));

    private static FatosResponse ToFatos(
        FatosConhecidos fatos,
        IReadOnlyDictionary<Guid, Evidencia>? evidencias) => new(
        ToFato(fatos.Origem, evidencias),
        ToFato(fatos.Condicao, evidencias),
        ToFato(fatos.Historico, evidencias));

    private static FatoResponse ToFato(
        BlocoFato fato,
        IReadOnlyDictionary<Guid, Evidencia>? evidencias) => new(
        fato.Tipo.ToContractValue(),
        fato.Indisponivel,
        fato.Descricao,
        fato.Fonte,
        ToEvidencia(fato.EvidenciaId, evidencias),
        fato.LimitacaoDeclarada,
        fato.AtendeTransparencia,
        ToAutoria(fato.AtualizadoPor));

    private static EvidenciaResponse? ToEvidencia(
        Guid? evidenciaId,
        IReadOnlyDictionary<Guid, Evidencia>? evidencias)
    {
        if (evidenciaId is not Guid id || evidencias is null || !evidencias.TryGetValue(id, out var evidencia))
        {
            return null;
        }

        return new EvidenciaResponse(
            evidencia.Id,
            evidencia.NomeArquivo,
            evidencia.TipoConteudo,
            evidencia.TamanhoBytes,
            evidencia.EnviadaEm);
    }

    private static DisponibilidadeResponse ToDisponibilidade(Disponibilidade disponibilidade) => new(
        disponibilidade.Estado.ToContractValue(),
        disponibilidade.Desde,
        ToAutoria(disponibilidade.AlteradaPor),
        disponibilidade.TransicoesPermitidas.Select(estado => estado.ToContractValue()).ToArray());

    private static LocalizacaoResponse ToLocalizacao(Localizacao localizacao) => new(
        localizacao.Cep,
        localizacao.Cidade,
        localizacao.Uf);

    private static AutoriaResponse ToAutoria(Autoria autoria) => new(autoria.UsuarioId, autoria.Nome, autoria.Em);

    private static string? JoinDescription(Veiculo veiculo)
    {
        var parts = new[] { veiculo.Marca, veiculo.Modelo, veiculo.Versao }
            .Where(value => !string.IsNullOrWhiteSpace(value));
        var description = string.Join(' ', parts);
        return string.IsNullOrWhiteSpace(description) ? null : description;
    }

    private static string PendingText(CodigoCriterio codigo) => codigo switch
    {
        CodigoCriterio.Identificacao => "Placa não informada",
        CodigoCriterio.DadosBasicos => "Dados básicos do veículo incompletos",
        CodigoCriterio.Localizacao => "Localização não informada",
        CodigoCriterio.PrecoOficial => "Preço oficial não definido",
        CodigoCriterio.Disponibilidade => "Disponibilidade não informada",
        CodigoCriterio.TransparenciaFatos => "Fatos conhecidos sem conteúdo ou limitação declarada",
        _ => "Critério mínimo não atendido",
    };
}
