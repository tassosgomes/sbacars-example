using System.Globalization;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Domain.Solicitacoes;

namespace SbaCars.Inventory.Application.Common;

public sealed record SolicitacaoResumoResponse(
    Guid SolicitacaoId,
    Guid OfertaId,
    string? Placa,
    string? DescricaoVeiculo,
    string Tipo,
    string Status,
    string? ValorVigente,
    string? ValorProposto,
    DateTimeOffset AbertaEm,
    AutoriaResponse AbertaPor,
    bool ForaDoSla);

public sealed record ContextoOfertaResponse(
    string Situacao,
    string Disponibilidade,
    PrecoOficialResponse? PrecoOficial,
    LocalizacaoResponse Localizacao,
    IReadOnlyCollection<string> BlocosComLimitacao);

public sealed record SolicitacaoDetalheResponse(
    Guid SolicitacaoId,
    Guid OfertaId,
    string? Placa,
    string? DescricaoVeiculo,
    string Tipo,
    string Status,
    string? ValorVigente,
    string? ValorProposto,
    DateTimeOffset AbertaEm,
    AutoriaResponse AbertaPor,
    bool ForaDoSla,
    string Justificativa,
    long? NovoPrecoCentavos,
    ChecklistElegibilidadeResponse? ElegibilidadeProposta,
    ContextoOfertaResponse ContextoOferta,
    string ImpactoAoAprovar,
    DecisaoResponse? Decisao,
    bool PodeDecidir);

public sealed record DecisaoResponse(
    string Status,
    DateTimeOffset DecididaEm,
    AutoriaResponse DecididaPor,
    string? Justificativa);

public sealed record ContagemPendentesResponse(
    int Total,
    IReadOnlyDictionary<string, int> PorTipo,
    int ForaDoSla);

/// <summary>
/// Database projection used by the queue. It intentionally contains only fields needed by the
/// list contract, so listing the queue never materializes an Oferta aggregate.
/// </summary>
public sealed record SolicitacaoFilaProjection(
    Guid SolicitacaoId,
    Guid OfertaId,
    string? Placa,
    string? Marca,
    string? Modelo,
    string? Versao,
    TipoSolicitacao Tipo,
    StatusSolicitacao Status,
    long? NovoPrecoCentavos,
    DateTimeOffset AbertaEm,
    string AbertaPorUsuarioId,
    string AbertaPorNome,
    long? PrecoVigenteCentavos,
    SituacaoOferta SituacaoOferta,
    EstadoDisponibilidade DisponibilidadeOferta);

public static class SolicitacaoResponseMapper
{
    public static SolicitacaoDetalheResponse ToDetalhe(
        Solicitacao solicitacao,
        Oferta oferta,
        CalculadoraDiasUteis calculadora,
        DateTimeOffset agora,
        string? currentUserId = null)
    {
        ArgumentNullException.ThrowIfNull(solicitacao);
        ArgumentNullException.ThrowIfNull(oferta);
        ArgumentNullException.ThrowIfNull(calculadora);

        var resumo = ToResumo(solicitacao, oferta, calculadora, agora);
        var ofertaDetalhe = OfertaResponseMapper.ToDetalhe(oferta);

        return new SolicitacaoDetalheResponse(
            resumo.SolicitacaoId,
            resumo.OfertaId,
            resumo.Placa,
            resumo.DescricaoVeiculo,
            resumo.Tipo,
            resumo.Status,
            resumo.ValorVigente,
            resumo.ValorProposto,
            resumo.AbertaEm,
            resumo.AbertaPor,
            resumo.ForaDoSla,
            solicitacao.Justificativa,
            solicitacao.NovoPrecoCentavos,
            solicitacao.Tipo == TipoSolicitacao.Elegibilidade
                ? ofertaDetalhe.Elegibilidade
                : null,
            new ContextoOfertaResponse(
                ofertaDetalhe.Situacao,
                ofertaDetalhe.Disponibilidade.Estado,
                ofertaDetalhe.PrecoOficial,
                ofertaDetalhe.Veiculo.Localizacao,
                new[]
                {
                    oferta.Fatos.Origem,
                    oferta.Fatos.Condicao,
                    oferta.Fatos.Historico,
                }
                .Where(fato => fato.Indisponivel)
                .Select(fato => fato.Tipo.ToContractValue())
                .ToArray()),
            Impacto(solicitacao.Tipo),
            ToDecisao(solicitacao.Decisao),
            PodeDecidir: solicitacao.Status == StatusSolicitacao.Pendente &&
                         !string.IsNullOrWhiteSpace(currentUserId) &&
                         !string.Equals(
                             solicitacao.AbertaPor.UsuarioId,
                             currentUserId,
                             StringComparison.Ordinal));
    }

    public static SolicitacaoResumoResponse ToResumo(
        Solicitacao solicitacao,
        Oferta oferta,
        CalculadoraDiasUteis calculadora,
        DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(solicitacao);
        ArgumentNullException.ThrowIfNull(oferta);
        ArgumentNullException.ThrowIfNull(calculadora);

        return new SolicitacaoResumoResponse(
            solicitacao.Id,
            solicitacao.OfertaId,
            oferta.Veiculo.Placa,
            JoinDescription(oferta.Veiculo),
            solicitacao.Tipo.ToContractValue(),
            solicitacao.Status.ToContractValue(),
            CurrentValue(solicitacao.Tipo, oferta),
            ProposedValue(solicitacao.Tipo, solicitacao.NovoPrecoCentavos),
            solicitacao.AbertaEm,
            ToAutoria(solicitacao.AbertaPor),
            solicitacao.Status == StatusSolicitacao.Pendente &&
            calculadora.ForaDoSla(solicitacao.AbertaEm, agora));
    }

    public static SolicitacaoResumoResponse ToResumo(
        SolicitacaoFilaProjection projection,
        CalculadoraDiasUteis calculadora,
        DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(calculadora);

        return new SolicitacaoResumoResponse(
            projection.SolicitacaoId,
            projection.OfertaId,
            projection.Placa,
            JoinDescription(projection.Marca, projection.Modelo, projection.Versao),
            projection.Tipo.ToContractValue(),
            projection.Status.ToContractValue(),
            CurrentValue(
                projection.Tipo,
                projection.SituacaoOferta.ToContractValue(),
                projection.DisponibilidadeOferta.ToContractValue(),
                projection.PrecoVigenteCentavos),
            ProposedValue(projection.Tipo, projection.NovoPrecoCentavos),
            projection.AbertaEm,
            new AutoriaResponse(
                projection.AbertaPorUsuarioId,
                projection.AbertaPorNome,
                projection.AbertaEm),
            projection.Status == StatusSolicitacao.Pendente &&
            calculadora.ForaDoSla(projection.AbertaEm, agora));
    }

    private static AutoriaResponse ToAutoria(Autoria autoria) =>
        new(autoria.UsuarioId, autoria.Nome, autoria.Em);

    private static DecisaoResponse? ToDecisao(Decisao? decisao) => decisao is null
        ? null
        : new DecisaoResponse(
            decisao.Status.ToContractValue(),
            decisao.DecididaEm,
            ToAutoria(decisao.DecididaPor),
            decisao.Justificativa);

    private static string? CurrentValue(TipoSolicitacao tipo, Oferta oferta) =>
        CurrentValue(
            tipo,
            oferta.Situacao.ToContractValue(),
            oferta.Disponibilidade.Estado.ToContractValue(),
            oferta.PrecoOficial?.ValorCentavos);

    private static string? CurrentValue(
        TipoSolicitacao tipo,
        string situacao,
        string disponibilidade,
        long? precoCentavos) => tipo switch
        {
            TipoSolicitacao.Elegibilidade => SituacaoLabel(situacao),
            TipoSolicitacao.Preco => precoCentavos is null ? null : FormatPrice(precoCentavos.Value),
            TipoSolicitacao.Retirada => SituacaoLabel(situacao),
            TipoSolicitacao.ReversaoVenda => DisponibilidadeLabel(disponibilidade),
            _ => null,
        };

    private static string? ProposedValue(TipoSolicitacao tipo, long? novoPrecoCentavos) => tipo switch
    {
        TipoSolicitacao.Elegibilidade => "Elegível",
        TipoSolicitacao.Preco when novoPrecoCentavos.HasValue => FormatPrice(novoPrecoCentavos.Value),
        TipoSolicitacao.Retirada => "Retirada",
        TipoSolicitacao.ReversaoVenda => "Disponível",
        _ => null,
    };

    private static string Impacto(TipoSolicitacao tipo) => tipo switch
    {
        TipoSolicitacao.Elegibilidade => "Ao aprovar, esta oferta passa a ser fornecida ao catálogo público em até 1 hora.",
        TipoSolicitacao.Preco => "Ao aprovar, o novo valor passa a ser o preço oficial vigente da oferta.",
        TipoSolicitacao.Retirada => "Ao aprovar, a oferta deixa de ser fornecida ao catálogo público; a disponibilidade permanece inalterada.",
        TipoSolicitacao.ReversaoVenda => "Ao aprovar, a disponibilidade da oferta passa de vendido para disponível.",
        _ => "A alteração será aplicada após a validação.",
    };

    private static string SituacaoLabel(string value) => value switch
    {
        "em-preparacao" => "Em preparação",
        "elegivel" => "Elegível",
        "suspensa" => "Suspensa",
        "retirada" => "Retirada",
        _ => value,
    };

    private static string DisponibilidadeLabel(string value) => value switch
    {
        "disponivel" => "Disponível",
        "reservado" => "Reservado",
        "vendido" => "Vendido",
        _ => value,
    };

    private static string FormatPrice(long centavos)
    {
        var reais = Math.DivRem(centavos, 100, out var centavosRestantes);
        var reaisFormatados = reais
            .ToString("N0", CultureInfo.InvariantCulture)
            .Replace(",", ".", StringComparison.Ordinal);
        return $"R$ {reaisFormatados},{centavosRestantes:00}";
    }

    private static string? JoinDescription(Veiculo veiculo)
        => JoinDescription(veiculo.Marca, veiculo.Modelo, veiculo.Versao);

    private static string? JoinDescription(string? marca, string? modelo, string? versao)
    {
        var parts = new[] { marca, modelo, versao }
            .Where(value => !string.IsNullOrWhiteSpace(value));
        var description = string.Join(' ', parts);
        return string.IsNullOrWhiteSpace(description) ? null : description;
    }
}
