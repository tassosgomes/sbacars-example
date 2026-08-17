using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using SbaCars.BuildingBlocks.Application;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Integracao;
using SbaCars.Inventory.Application.Solicitacoes.AprovarSolicitacao;
using SbaCars.Inventory.Application.Solicitacoes.ObterSolicitacao;
using SbaCars.Inventory.Application.Solicitacoes.RejeitarSolicitacao;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Domain.Solicitacoes;

namespace SbaCars.Inventory.UnitTests.Solicitacoes;

public sealed class DecisaoSolicitacaoTests
{
    private static readonly DateTimeOffset OpenedAt =
        new(2026, 8, 17, 9, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset DecidedAt =
        new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Aprovar_ElegibilidadeComCriteriosAindaAtendidos_TornaOfertaElegivel()
    {
        var oferta = CreateCompleteOffer(SituacaoOferta.EmPreparacao);
        var solicitacao = CreateRequest(oferta, TipoSolicitacao.Elegibilidade);
        var handler = CreateApprovalHandler(oferta, solicitacao, "validator-1", "Bruno");

        var response = await handler.HandleAsync(
            new AprovarSolicitacaoCommand { SolicitacaoId = solicitacao.Id },
            CancellationToken.None);

        oferta.Situacao.Should().Be(SituacaoOferta.Elegivel);
        solicitacao.Status.Should().Be(StatusSolicitacao.Aprovada);
        solicitacao.Decisao.Should().NotBeNull();
        solicitacao.Decisao!.DecididaPor.UsuarioId.Should().Be("validator-1");
        solicitacao.Decisao.DecididaEm.Should().Be(DecidedAt);
        response.Status.Should().Be("aprovada");
        response.PodeDecidir.Should().BeFalse();
    }

    [Fact]
    public async Task Aprovar_PeloMesmoUsuarioQueAbriu_LancaAutoAprovacaoESemMutar()
    {
        var oferta = CreateCompleteOffer(SituacaoOferta.EmPreparacao);
        var solicitacao = CreateRequest(oferta, TipoSolicitacao.Elegibilidade);
        var handler = CreateApprovalHandler(oferta, solicitacao, "operator-1", "Ana");

        var act = () => handler.HandleAsync(
            new AprovarSolicitacaoCommand { SolicitacaoId = solicitacao.Id },
            CancellationToken.None);

        await act.Should().ThrowAsync<AutoAprovacaoException>();
        solicitacao.Status.Should().Be(StatusSolicitacao.Pendente);
        oferta.Situacao.Should().Be(SituacaoOferta.EmPreparacao);
    }

    [Fact]
    public async Task Aprovar_Retirada_MantemDisponibilidadeVigente()
    {
        var oferta = CreateCompleteOffer(SituacaoOferta.Elegivel);
        var estadoAntes = oferta.Disponibilidade.Estado;
        var solicitacao = CreateRequest(oferta, TipoSolicitacao.Retirada);
        var handler = CreateApprovalHandler(oferta, solicitacao, "validator-1", "Bruno");

        await handler.HandleAsync(
            new AprovarSolicitacaoCommand { SolicitacaoId = solicitacao.Id },
            CancellationToken.None);

        oferta.Situacao.Should().Be(SituacaoOferta.Retirada);
        oferta.Disponibilidade.Estado.Should().Be(estadoAntes);
        solicitacao.Status.Should().Be(StatusSolicitacao.Aprovada);
    }

    [Fact]
    public async Task Aprovar_Preco_SubstituiVigenteSomenteNaDecisao()
    {
        var oferta = CreateCompleteOffer(SituacaoOferta.EmPreparacao);
        var solicitacao = CreateRequest(oferta, TipoSolicitacao.Preco, 8_450_000);
        var handler = CreateApprovalHandler(oferta, solicitacao, "validator-1", "Bruno");

        oferta.PrecoOficial!.ValorCentavos.Should().Be(8_790_000);

        await handler.HandleAsync(
            new AprovarSolicitacaoCommand { SolicitacaoId = solicitacao.Id },
            CancellationToken.None);

        oferta.PrecoOficial!.ValorCentavos.Should().Be(8_450_000);
        oferta.PrecoOficial.DefinidoPor.UsuarioId.Should().Be("validator-1");
        solicitacao.Status.Should().Be(StatusSolicitacao.Aprovada);
    }

    [Fact]
    public async Task Rejeitar_ComJustificativa_PreservaOfertaERegistraDecisao()
    {
        var oferta = CreateCompleteOffer(SituacaoOferta.Elegivel);
        var solicitacao = CreateRequest(oferta, TipoSolicitacao.Retirada);
        var handler = CreateRejectionHandler(oferta, solicitacao, "validator-1", "Bruno");

        var response = await handler.HandleAsync(
            new RejeitarSolicitacaoCommand
            {
                SolicitacaoId = solicitacao.Id,
                Justificativa = "Falta confirmar a origem do veículo.",
            },
            CancellationToken.None);

        oferta.Situacao.Should().Be(SituacaoOferta.Elegivel);
        solicitacao.Status.Should().Be(StatusSolicitacao.Rejeitada);
        solicitacao.Decisao!.Justificativa.Should().Be("Falta confirmar a origem do veículo.");
        response.Decisao!.Status.Should().Be("rejeitada");
    }

    [Fact]
    public async Task Rejeitar_SemJustificativa_LancaExcecaoESemAlterarEstado()
    {
        var oferta = CreateCompleteOffer(SituacaoOferta.Elegivel);
        var solicitacao = CreateRequest(oferta, TipoSolicitacao.Retirada);
        var handler = CreateRejectionHandler(oferta, solicitacao, "validator-1", "Bruno");

        var act = () => handler.HandleAsync(
            new RejeitarSolicitacaoCommand
            {
                SolicitacaoId = solicitacao.Id,
                Justificativa = null,
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<JustificativaRejeicaoObrigatoriaException>();
        solicitacao.Status.Should().Be(StatusSolicitacao.Pendente);
        oferta.Situacao.Should().Be(SituacaoOferta.Elegivel);
    }

    [Fact]
    public async Task Aprovar_SolicitacaoJaDecidida_RetornaConflitoDeDominio()
    {
        var oferta = CreateCompleteOffer(SituacaoOferta.EmPreparacao);
        var solicitacao = CreateRequest(oferta, TipoSolicitacao.Elegibilidade);
        solicitacao.Aprovar(
            new Autoria("validator-1", "Bruno", DecidedAt),
            DecidedAt);
        var handler = CreateApprovalHandler(oferta, solicitacao, "validator-2", "Carla");

        var act = () => handler.HandleAsync(
            new AprovarSolicitacaoCommand { SolicitacaoId = solicitacao.Id },
            CancellationToken.None);

        await act.Should().ThrowAsync<SolicitacaoJaDecididaException>();
    }

    [Fact]
    public async Task ObterSolicitacao_OutroValidadorPodeDecidirEExpõeContexto()
    {
        var oferta = CreateCompleteOffer(SituacaoOferta.Elegivel);
        var solicitacao = CreateRequest(oferta, TipoSolicitacao.Retirada);
        var handler = new ObterSolicitacaoHandler(
            new FakeSolicitacaoRepository(solicitacao),
            new FakeOfertaRepository(oferta),
            new CalculadoraDiasUteis(),
            new FakeCurrentUser("validator-1", "Bruno"),
            new FixedClock(DecidedAt));

        var response = await handler.HandleAsync(
            new ObterSolicitacaoQuery(solicitacao.Id),
            CancellationToken.None);

        response.PodeDecidir.Should().BeTrue();
        response.ImpactoAoAprovar.Should().Contain("disponibilidade permanece inalterada");
        response.ContextoOferta.Situacao.Should().Be("elegivel");
        response.ValorVigente.Should().Be("Elegível");
        response.ValorProposto.Should().Be("Retirada");
        response.ForaDoSla.Should().BeFalse();
    }

    [Fact]
    public void RejeitarValidator_SemJustificativa_RetornaErroDeRequisicao()
    {
        var result = new RejeitarSolicitacaoValidator().Validate(
            new RejeitarSolicitacaoCommand { SolicitacaoId = Guid.NewGuid() });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == "Justificativa");
    }

    private static AprovarSolicitacaoHandler CreateApprovalHandler(
        Oferta oferta,
        Solicitacao solicitacao,
        string userId,
        string name) => new(
        new FakeSolicitacaoRepository(solicitacao),
        new FakeOfertaRepository(oferta),
        new FakeUnitOfWork(),
        new FakeIntegrationEventPublisher(),
        new FakeCurrentUser(userId, name),
        new FixedClock(DecidedAt),
        new CalculadoraDiasUteis(),
        NullLogger<AprovarSolicitacaoHandler>.Instance);

    private static RejeitarSolicitacaoHandler CreateRejectionHandler(
        Oferta oferta,
        Solicitacao solicitacao,
        string userId,
        string name) => new(
        new FakeSolicitacaoRepository(solicitacao),
        new FakeOfertaRepository(oferta),
        new FakeUnitOfWork(),
        new FakeCurrentUser(userId, name),
        new FixedClock(DecidedAt),
        new CalculadoraDiasUteis(),
        NullLogger<RejeitarSolicitacaoHandler>.Instance);

    private static Solicitacao CreateRequest(
        Oferta oferta,
        TipoSolicitacao tipo,
        long? novoPrecoCentavos = null) => Solicitacao.Abrir(
        oferta.Id,
        tipo,
        novoPrecoCentavos,
        "Solicitação operacional.",
        new Autoria("operator-1", "Ana", OpenedAt),
        OpenedAt);

    private static Oferta CreateCompleteOffer(SituacaoOferta situacao)
    {
        var autoria = new Autoria("operator-1", "Ana", OpenedAt);
        var oferta = Oferta.Criar(
            new Veiculo(
                TipoVeiculo.CarroSeminovo,
                placa: "ABC1D23",
                marca: "Honda",
                modelo: "Civic",
                versao: "EXL",
                anoFabricacao: 2021,
                anoModelo: 2022,
                quilometragem: 48_300,
                cambio: "Automático",
                localizacao: new Localizacao("13010-111", "Campinas", "SP")),
            autoria,
            OpenedAt);

        SetProperty(oferta, nameof(Oferta.Fatos), FatosConhecidos.Criar(
            new BlocoFato(BlocoFatoTipo.Origem, descricao: "Origem conhecida", atualizadoPor: autoria),
            new BlocoFato(BlocoFatoTipo.Condicao, descricao: "Condição conhecida", atualizadoPor: autoria),
            new BlocoFato(BlocoFatoTipo.Historico, descricao: "Histórico conhecido", atualizadoPor: autoria)));
        oferta.DefinirPrecoInicial(8_790_000, autoria.UsuarioId, autoria.Nome, OpenedAt);
        SetProperty(oferta.Disponibilidade, nameof(Disponibilidade.EstadoConhecido), true);
        SetProperty(oferta, nameof(Oferta.Situacao), situacao);
        return oferta;
    }

    private static void SetProperty<T>(T target, string propertyName, object? value) =>
        typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(target, value);

    private sealed class FakeSolicitacaoRepository(Solicitacao solicitacao) : ISolicitacaoRepository
    {
        public Task<Solicitacao?> ObterAsync(
            Guid solicitacaoId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Solicitacao?>(solicitacao.Id == solicitacaoId ? solicitacao : null);

        public Task<bool> ExistePendenteAsync(
            Guid ofertaId,
            TipoSolicitacao tipo,
            CancellationToken cancellationToken = default) => Task.FromResult(false);

        public void Adicionar(Solicitacao entity) => throw new NotSupportedException();
    }

    private sealed class FakeOfertaRepository(Oferta oferta) : IOfertaRepository
    {
        public Task<Oferta?> ObterAsync(
            Guid ofertaId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Oferta?>(oferta.Id == ofertaId ? oferta : null);

        public Task<bool> ExistePlacaAtivaAsync(
            string placa,
            Guid? ignorarOfertaId = null,
            CancellationToken cancellationToken = default) => Task.FromResult(false);

        public void Adicionar(Oferta entity) => throw new NotSupportedException();

        public void Remover(Oferta entity) => throw new NotSupportedException();
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1);
    }

    private sealed class FakeIntegrationEventPublisher : IEstoqueIntegrationEventPublisher
    {
        public Task PublishOfferIncludedAsync(
            Guid ofertaId,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PublishOfferUpdatedAsync(
            Guid ofertaId,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PublishOfferWithdrawnAsync(
            Guid ofertaId,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PublishAvailabilityChangedAsync(
            Guid ofertaId,
            string disponibilidade,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class FakeCurrentUser(string userId, string displayName) : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public string? UserId => userId;

        public string? DisplayName => displayName;

        public IReadOnlyCollection<string> Permissions => ["estoque:validar"];

        public bool HasPermission(string permission) =>
            string.Equals(permission, "estoque:validar", StringComparison.Ordinal);
    }
}
