using SbaCars.BuildingBlocks.Domain;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Domain.Solicitacoes;

/// <summary>
/// A validation request is a separate aggregate from Oferta. It keeps the queue directly
/// queryable while retaining only an id reference to the offer (ADR-002).
/// </summary>
public sealed class Solicitacao : AggregateRoot
{
    private Solicitacao()
    {
        AbertaPor = Autoria.System(DateTimeOffset.UnixEpoch);
        Justificativa = string.Empty;
    }

    private Solicitacao(
        Guid ofertaId,
        TipoSolicitacao tipo,
        long? novoPrecoCentavos,
        string justificativa,
        Autoria abertaPor,
        DateTimeOffset abertaEm)
    {
        if (ofertaId == Guid.Empty)
        {
            throw new ArgumentException("O id da oferta é obrigatório.", nameof(ofertaId));
        }

        ArgumentNullException.ThrowIfNull(abertaPor);
        ArgumentException.ThrowIfNullOrWhiteSpace(justificativa);

        OfertaId = ofertaId;
        Tipo = tipo;
        Status = StatusSolicitacao.Pendente;
        NovoPrecoCentavos = novoPrecoCentavos;
        Justificativa = justificativa.Trim();
        AbertaEm = abertaEm.ToUniversalTime();
        AbertaPor = abertaPor.Copiar();
    }

    public Guid OfertaId { get; private set; }

    public TipoSolicitacao Tipo { get; private set; }

    public StatusSolicitacao Status { get; private set; }

    public long? NovoPrecoCentavos { get; private set; }

    public string Justificativa { get; private set; }

    public DateTimeOffset AbertaEm { get; private set; }

    public Autoria AbertaPor { get; private set; }

    public Decisao? Decisao { get; private set; }

    public static Solicitacao Abrir(
        Guid ofertaId,
        TipoSolicitacao tipo,
        long? novoPrecoCentavos,
        string justificativa,
        Autoria abertaPor,
        DateTimeOffset abertaEm)
    {
        if (tipo == TipoSolicitacao.Preco && (novoPrecoCentavos is null or <= 0))
        {
            throw new PrecoSolicitacaoObrigatorioException();
        }

        if (tipo != TipoSolicitacao.Preco && novoPrecoCentavos is not null)
        {
            throw new CampoPrecoSolicitacaoNaoPermitidoException(tipo);
        }

        return new Solicitacao(
            ofertaId,
            tipo,
            novoPrecoCentavos,
            justificativa,
            abertaPor,
            abertaEm);
    }

    /// <summary>
    /// Records an approval. Applying the requested change belongs to the application use case;
    /// this aggregate owns only the request lifecycle and its decision audit record.
    /// </summary>
    public void Aprovar(
        Autoria decididaPor,
        DateTimeOffset decididaEm,
        string? observacao = null)
    {
        EnsurePendente();
        var decisao = global::SbaCars.Inventory.Domain.Solicitacoes.Decisao.Aprovar(
            decididaPor,
            decididaEm,
            observacao);
        Status = StatusSolicitacao.Aprovada;
        Decisao = decisao;
    }

    /// <summary>Records a rejection and preserves the requested offer state.</summary>
    public void Rejeitar(
        Autoria decididaPor,
        DateTimeOffset decididaEm,
        string justificativa)
    {
        EnsurePendente();
        var decisao = global::SbaCars.Inventory.Domain.Solicitacoes.Decisao.Rejeitar(
            decididaPor,
            decididaEm,
            justificativa);
        Status = StatusSolicitacao.Rejeitada;
        Decisao = decisao;
    }

    private void EnsurePendente()
    {
        if (Status != StatusSolicitacao.Pendente)
        {
            throw new SolicitacaoJaDecididaException(Id);
        }
    }

}
