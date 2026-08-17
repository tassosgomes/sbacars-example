using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Ofertas;

/// <summary>
/// Aggregate root for the curated vehicle offer. The first slice deliberately accepts partial
/// vehicle data and keeps the offer in preparation; later slices add the remaining transitions.
/// </summary>
public sealed class Oferta : AggregateRoot
{
    private Oferta()
    {
        Veiculo = new Veiculo(TipoVeiculo.CarroUsado);
        CriadaPor = Autoria.System(DateTimeOffset.UnixEpoch);
        AtualizadaPor = CriadaPor.Copiar();
        Disponibilidade = Disponibilidade.Inicial(CriadaPor);
        Fatos = FatosConhecidos.Vazios();
    }

    private Oferta(Veiculo veiculo, Autoria autoria, DateTimeOffset agora)
        : base()
    {
        Veiculo = veiculo;
        Situacao = SituacaoOferta.EmPreparacao;
        CriadaEm = agora.ToUniversalTime();
        AtualizadoEm = CriadaEm;
        // EF Core requires each owned navigation to have a distinct instance, even when the
        // value objects contain the same authorship values.
        CriadaPor = autoria.Copiar();
        AtualizadaPor = autoria.Copiar();
        Disponibilidade = Disponibilidade.Inicial(autoria);
        Fatos = FatosConhecidos.Vazios();
    }

    public Veiculo Veiculo { get; private set; }

    public SituacaoOferta Situacao { get; private set; }

    public string? MotivoSuspensao { get; private set; }

    public DateTimeOffset? SuspensaEm { get; private set; }

    public FatosConhecidos Fatos { get; private set; }

    public PrecoOficial? PrecoOficial { get; private set; }

    public Disponibilidade Disponibilidade { get; private set; }

    public DateTimeOffset CriadaEm { get; private set; }

    public DateTimeOffset AtualizadoEm { get; private set; }

    public Autoria CriadaPor { get; private set; }

    public Autoria AtualizadaPor { get; private set; }

    public static Oferta Criar(Veiculo veiculo, Autoria autoria, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(veiculo);
        ArgumentNullException.ThrowIfNull(autoria);

        if (veiculo.TipoVeiculo is not (TipoVeiculo.CarroSeminovo or TipoVeiculo.CarroUsado))
        {
            throw new Exceptions.TipoVeiculoNaoPermitidoException(veiculo.TipoVeiculo.ToString());
        }

        return new Oferta(veiculo, autoria, agora);
    }

    /// <summary>Returns the six minimum criteria; a populated list means the criteria are missing.</summary>
    public IReadOnlyList<CodigoCriterio> AvaliarCriteriosMinimos()
    {
        return AvaliarCriteriosMinimos(Veiculo);
    }

    /// <summary>
    /// Applies a partial vehicle update only after evaluating its candidate state. An eligible
    /// offer that would lose criteria requires explicit confirmation; until then no aggregate
    /// member is mutated, which makes the 409 path a logical rollback as well as a persistence
    /// rollback.
    /// </summary>
    public void AtualizarVeiculo(
        VeiculoPatch patch,
        Autoria autoria,
        DateTimeOffset agora,
        bool confirmaSuspensao)
    {
        ArgumentNullException.ThrowIfNull(patch);
        ArgumentNullException.ThrowIfNull(autoria);

        if (patch.PlacaInformada && Situacao != SituacaoOferta.EmPreparacao)
        {
            throw new Exceptions.PlacaImutavelException();
        }

        var veiculoAtualizado = Veiculo.ComAlteracao(patch);
        var ofertaEraElegivel = Situacao == SituacaoOferta.Elegivel;
        var criteriosAfetados = ofertaEraElegivel
            ? AvaliarCriteriosMinimos(veiculoAtualizado)
            : [];

        if (criteriosAfetados.Count > 0 && !confirmaSuspensao)
        {
            throw new Exceptions.SuspensaoNaoConfirmadaException(criteriosAfetados);
        }

        Veiculo.Aplicar(patch);
        AtualizadoEm = agora.ToUniversalTime();
        AtualizadaPor = autoria.Copiar();

        if (ofertaEraElegivel && criteriosAfetados.Count > 0)
        {
            Situacao = SituacaoOferta.Suspensa;
            MotivoSuspensao = BuildSuspensionReason(criteriosAfetados);
            SuspensaEm = AtualizadoEm;
        }
    }

    /// <summary>
    /// Replaces the three known-fact blocks as one aggregate mutation. When an eligible offer
    /// would lose CM-6, the candidate is evaluated before this aggregate is changed so the
    /// unconfirmed 409 path leaves both the domain object and the persistence tracker untouched.
    /// </summary>
    public void SubstituirFatos(
        FatosConhecidos fatos,
        Autoria autoria,
        DateTimeOffset agora,
        bool confirmaSuspensao)
    {
        ArgumentNullException.ThrowIfNull(fatos);
        ArgumentNullException.ThrowIfNull(autoria);

        var ofertaEraElegivel = Situacao == SituacaoOferta.Elegivel;
        var criteriosAfetados = ofertaEraElegivel
            ? AvaliarCriteriosMinimos(Veiculo, fatos)
            : [];

        if (criteriosAfetados.Count > 0 && !confirmaSuspensao)
        {
            throw new Exceptions.SuspensaoNaoConfirmadaException(criteriosAfetados);
        }

        Fatos = fatos;
        AtualizadoEm = agora.ToUniversalTime();
        AtualizadaPor = autoria.Copiar();

        if (ofertaEraElegivel && criteriosAfetados.Count > 0)
        {
            Situacao = SituacaoOferta.Suspensa;
            MotivoSuspensao = BuildSuspensionReason(criteriosAfetados);
            SuspensaEm = AtualizadoEm;
        }
    }

    /// <summary>
    /// Defines the first official price directly. Once a price exists, later changes must go
    /// through the validated request flow and therefore cannot overwrite this value.
    /// </summary>
    public void DefinirPrecoInicial(
        long valorCentavos,
        string usuarioId,
        string? nomeUsuario,
        DateTimeOffset agora)
    {
        if (PrecoOficial is not null)
        {
            throw new Exceptions.PrecoJaDefinidoException();
        }

        var autoria = new Autoria(usuarioId, nomeUsuario, agora);
        PrecoOficial = new PrecoOficial(valorCentavos, autoria);
        AtualizadoEm = agora.ToUniversalTime();
        AtualizadaPor = autoria.Copiar();
    }

    /// <summary>
    /// Applies a previously validated price proposal. The current price remains untouched until
    /// this method is called by the approval use case.
    /// </summary>
    public void AplicarAlteracaoDePreco(
        long valorCentavos,
        Autoria autoria,
        DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(autoria);

        if (PrecoOficial is null)
        {
            throw new Exceptions.PrecoVigenteNaoDefinidoException();
        }

        PrecoOficial = new PrecoOficial(valorCentavos, autoria);
        AtualizadoEm = agora.ToUniversalTime();
        AtualizadaPor = autoria.Copiar();
    }

    /// <summary>
    /// Makes the offer eligible after re-evaluating all criteria at decision time. This also
    /// supports reinclusion of a previously withdrawn offer.
    /// </summary>
    public void TornarElegivel(Autoria autoria, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(autoria);

        if (Situacao == SituacaoOferta.Elegivel)
        {
            throw new Exceptions.OfertaJaElegivelException();
        }

        var criterios = AvaliarCriteriosMinimos();
        if (criterios.Count > 0)
        {
            throw new Exceptions.CriteriosMinimosNaoAtendidosException(criterios);
        }

        Situacao = SituacaoOferta.Elegivel;
        MotivoSuspensao = null;
        SuspensaEm = null;
        AtualizadoEm = agora.ToUniversalTime();
        AtualizadaPor = autoria.Copiar();
    }

    /// <summary>
    /// Removes the offer from the curated catalog without changing its independent operational
    /// availability state.
    /// </summary>
    public void Retirar(Autoria autoria, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(autoria);

        if (Situacao == SituacaoOferta.Retirada)
        {
            throw new Exceptions.OfertaJaRetiradaException();
        }

        Situacao = SituacaoOferta.Retirada;
        AtualizadoEm = agora.ToUniversalTime();
        AtualizadaPor = autoria.Copiar();
    }

    /// <summary>
    /// Records an explicit operational availability transition without coupling it to the
    /// offer's curatorial situation. The optional observation belongs to the request contract;
    /// the MVP does not expose an availability history, so it is intentionally not logged here.
    /// </summary>
    public void AlterarDisponibilidade(
        EstadoDisponibilidade novoEstado,
        string? observacao,
        Autoria autoria,
        DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(autoria);

        // Keep the free-form observation out of logs and the current availability projection.
        // Historical observations are outside the MVP's availability value object.
        _ = observacao;

        Disponibilidade.Alterar(novoEstado, autoria, agora);
        AtualizadoEm = agora.ToUniversalTime();
        AtualizadaPor = autoria.Copiar();
    }

    /// <summary>Applies a sold-to-available transition after an approved reversal request.</summary>
    public void ReverterVenda(Autoria autoria, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(autoria);

        Disponibilidade.ReverterVenda(autoria, agora);
        AtualizadoEm = agora.ToUniversalTime();
        AtualizadaPor = autoria.Copiar();
    }

    public void Excluir()
    {
        if (Situacao != SituacaoOferta.EmPreparacao)
        {
            throw new Exceptions.OfertaNaoExcluivelException();
        }
    }

    private IReadOnlyList<CodigoCriterio> AvaliarCriteriosMinimos(Veiculo veiculo)
        => AvaliarCriteriosMinimos(veiculo, Fatos);

    private IReadOnlyList<CodigoCriterio> AvaliarCriteriosMinimos(
        Veiculo veiculo,
        FatosConhecidos fatos)
    {
        var faltantes = new List<CodigoCriterio>();

        if (string.IsNullOrWhiteSpace(veiculo.Placa))
        {
            faltantes.Add(CodigoCriterio.Identificacao);
        }

        if (string.IsNullOrWhiteSpace(veiculo.Marca) ||
            string.IsNullOrWhiteSpace(veiculo.Modelo) ||
            string.IsNullOrWhiteSpace(veiculo.Versao) ||
            veiculo.AnoFabricacao is null ||
            veiculo.Quilometragem is null ||
            string.IsNullOrWhiteSpace(veiculo.Cambio))
        {
            faltantes.Add(CodigoCriterio.DadosBasicos);
        }

        if (!veiculo.Localizacao.EstaCompleta)
        {
            faltantes.Add(CodigoCriterio.Localizacao);
        }

        if (PrecoOficial is null)
        {
            faltantes.Add(CodigoCriterio.PrecoOficial);
        }

        if (Disponibilidade is null || !Disponibilidade.EstadoConhecido)
        {
            faltantes.Add(CodigoCriterio.Disponibilidade);
        }

        if (!fatos.AtendeTransparencia)
        {
            faltantes.Add(CodigoCriterio.TransparenciaFatos);
        }

        return faltantes;
    }

    private static string BuildSuspensionReason(IReadOnlyCollection<CodigoCriterio> criterios) =>
        $"A alteração deixou de atender aos critérios: {string.Join(", ", criterios.Select(codigo => codigo.ToContractValue()))}.";
}
