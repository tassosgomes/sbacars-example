using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Integracao;
using SbaCars.Inventory.Application.Solicitacoes.AprovarSolicitacao;
using SbaCars.Inventory.Application.Solicitacoes.RejeitarSolicitacao;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Domain.Solicitacoes;
using SbaCars.Inventory.Infrastructure;
using SbaCars.Inventory.Infrastructure.Ofertas;
using SbaCars.Inventory.Infrastructure.Solicitacoes;
using SbaCars.TestKit;

using InventoryTestRepositories = SbaCars.Inventory.IntegrationTests.InventoryTestRepositories;

namespace SbaCars.Inventory.IntegrationTests.Solicitacoes;

[Collection(InventoryPostgresCollection.Name)]
public sealed class SolicitacaoDecisaoTests
{
    private readonly SbaCarsPostgresFixture _fixture;

    public SolicitacaoDecisaoTests(SbaCarsPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AprovarElegibilidade_PersisteOfertaElegivelEDecisaoNaMesmaUnidade()
    {
        await EnsureSchemaAsync();
        await ClearDataAsync();

        var now = new DateTimeOffset(2026, 8, 17, 9, 0, 0, TimeSpan.Zero);
        var oferta = CreateCompleteOffer(now, "ABC1D23", SituacaoOferta.EmPreparacao);
        var solicitacao = CreateRequest(oferta, TipoSolicitacao.Elegibilidade, now);

        await using (var setup = CreateContext())
        {
            setup.Ofertas.Add(oferta);
            setup.Solicitacoes.Add(solicitacao);
            await setup.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var handler = CreateApprovalHandler(context, "validator-1", "Bruno", now.AddHours(2));

            await handler.HandleAsync(
                new AprovarSolicitacaoCommand { SolicitacaoId = solicitacao.Id },
                CancellationToken.None);
        }

        await using var read = CreateContext();
        var persistedOffer = await read.Ofertas.AsNoTracking().SingleAsync(item => item.Id == oferta.Id);
        var persistedRequest = await read.Solicitacoes.AsNoTracking().SingleAsync(item => item.Id == solicitacao.Id);

        persistedOffer.Situacao.Should().Be(SituacaoOferta.Elegivel);
        persistedRequest.Status.Should().Be(StatusSolicitacao.Aprovada);
        persistedRequest.Decisao.Should().NotBeNull();
        persistedRequest.Decisao!.DecididaPor.UsuarioId.Should().Be("validator-1");
        persistedRequest.Decisao.DecididaEm.Should().Be(now.AddHours(2));
    }

    [Fact]
    public async Task AprovarRetirada_NaoAlteraDisponibilidadePersistida()
    {
        await EnsureSchemaAsync();
        await ClearDataAsync();

        var now = new DateTimeOffset(2026, 8, 17, 9, 0, 0, TimeSpan.Zero);
        var oferta = CreateCompleteOffer(now, "DEF4G56", SituacaoOferta.Elegivel);
        var solicitacao = CreateRequest(oferta, TipoSolicitacao.Retirada, now);

        await using (var setup = CreateContext())
        {
            setup.Ofertas.Add(oferta);
            setup.Solicitacoes.Add(solicitacao);
            await setup.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var handler = CreateApprovalHandler(context, "validator-1", "Bruno", now.AddHours(1));

            await handler.HandleAsync(
                new AprovarSolicitacaoCommand { SolicitacaoId = solicitacao.Id },
                CancellationToken.None);
        }

        await using var read = CreateContext();
        var persisted = await read.Ofertas.AsNoTracking().SingleAsync(item => item.Id == oferta.Id);

        persisted.Situacao.Should().Be(SituacaoOferta.Retirada);
        persisted.Disponibilidade.Estado.Should().Be(EstadoDisponibilidade.Disponivel);
    }

    [Fact]
    public async Task AutoAprovacao_ComMesmoUsuario_RecusaEDeixaSolicitacaoPendente()
    {
        await EnsureSchemaAsync();
        await ClearDataAsync();

        var now = new DateTimeOffset(2026, 8, 17, 9, 0, 0, TimeSpan.Zero);
        var oferta = CreateCompleteOffer(now, "GHI7H89", SituacaoOferta.EmPreparacao);
        var solicitacao = CreateRequest(oferta, TipoSolicitacao.Elegibilidade, now);

        await using (var setup = CreateContext())
        {
            setup.Ofertas.Add(oferta);
            setup.Solicitacoes.Add(solicitacao);
            await setup.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var handler = CreateApprovalHandler(context, "operator-1", "Ana", now.AddHours(1));
            var act = () => handler.HandleAsync(
                new AprovarSolicitacaoCommand { SolicitacaoId = solicitacao.Id },
                CancellationToken.None);

            await act.Should().ThrowAsync<AutoAprovacaoException>();
        }

        await using var read = CreateContext();
        var persisted = await read.Solicitacoes.AsNoTracking().SingleAsync(item => item.Id == solicitacao.Id);
        persisted.Status.Should().Be(StatusSolicitacao.Pendente);
        persisted.Decisao.Should().BeNull();
    }

    [Fact]
    public async Task Rejeitar_ComJustificativa_PreservaOfertaEGravaMotivoSemExporEmLog()
    {
        await EnsureSchemaAsync();
        await ClearDataAsync();

        var now = new DateTimeOffset(2026, 8, 17, 9, 0, 0, TimeSpan.Zero);
        var oferta = CreateCompleteOffer(now, "JKL0M12", SituacaoOferta.Elegivel);
        var solicitacao = CreateRequest(oferta, TipoSolicitacao.Preco, now, 8_450_000);

        await using (var setup = CreateContext())
        {
            setup.Ofertas.Add(oferta);
            setup.Solicitacoes.Add(solicitacao);
            await setup.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var handler = new RejeitarSolicitacaoHandler(
                new SolicitacaoRepository(context, new CalculadoraDiasUteis()),
                InventoryTestRepositories.CreateOfertaRepository(context),
                new ContextUnitOfWork(context),
                new StubCurrentUser("validator-1", "Bruno"),
                new FixedClock(now.AddHours(1)),
                new CalculadoraDiasUteis(),
                NullLogger<RejeitarSolicitacaoHandler>.Instance);

            await handler.HandleAsync(
                new RejeitarSolicitacaoCommand
                {
                    SolicitacaoId = solicitacao.Id,
                    Justificativa = "Valor proposto sem evidência de mercado.",
                },
                CancellationToken.None);
        }

        await using var read = CreateContext();
        var persistedOffer = await read.Ofertas.AsNoTracking().SingleAsync(item => item.Id == oferta.Id);
        var persistedRequest = await read.Solicitacoes.AsNoTracking().SingleAsync(item => item.Id == solicitacao.Id);

        persistedOffer.Situacao.Should().Be(SituacaoOferta.Elegivel);
        persistedOffer.PrecoOficial!.ValorCentavos.Should().Be(8_790_000);
        persistedRequest.Status.Should().Be(StatusSolicitacao.Rejeitada);
        persistedRequest.Decisao!.Justificativa.Should().Be("Valor proposto sem evidência de mercado.");
    }

    private AprovarSolicitacaoHandler CreateApprovalHandler(
        InventoryDbContext context,
        string userId,
        string displayName,
        DateTimeOffset now) => new(
        new SolicitacaoRepository(context, new CalculadoraDiasUteis()),
        InventoryTestRepositories.CreateOfertaRepository(context),
        new ContextUnitOfWork(context),
        new FakeIntegrationEventPublisher(),
        new StubCurrentUser(userId, displayName),
        new FixedClock(now),
        new CalculadoraDiasUteis(),
        NullLogger<AprovarSolicitacaoHandler>.Instance);

    private static Solicitacao CreateRequest(
        Oferta oferta,
        TipoSolicitacao tipo,
        DateTimeOffset now,
        long? novoPrecoCentavos = null) => Solicitacao.Abrir(
        oferta.Id,
        tipo,
        novoPrecoCentavos,
        "Solicitação operacional.",
        new Autoria("operator-1", "Ana", now),
        now);

    private static Oferta CreateCompleteOffer(
        DateTimeOffset now,
        string plate,
        SituacaoOferta situacao)
    {
        var autoria = new Autoria("operator-1", "Ana", now);
        var oferta = Oferta.Criar(
            new Veiculo(
                TipoVeiculo.CarroSeminovo,
                placa: plate,
                marca: "Honda",
                modelo: "Civic",
                versao: "EXL",
                anoFabricacao: 2021,
                anoModelo: 2022,
                quilometragem: 48_300,
                cambio: "Automático",
                localizacao: new Localizacao("13010-111", "Campinas", "SP")),
            autoria,
            now);

        SetProperty(oferta, nameof(Oferta.Fatos), FatosConhecidos.Criar(
            new BlocoFato(BlocoFatoTipo.Origem, descricao: "Origem conhecida", atualizadoPor: autoria),
            new BlocoFato(BlocoFatoTipo.Condicao, descricao: "Condição conhecida", atualizadoPor: autoria),
            new BlocoFato(BlocoFatoTipo.Historico, descricao: "Histórico conhecido", atualizadoPor: autoria)));
        oferta.DefinirPrecoInicial(8_790_000, autoria.UsuarioId, autoria.Nome, now);
        SetProperty(oferta.Disponibilidade, nameof(Disponibilidade.EstadoConhecido), true);
        SetProperty(oferta, nameof(Oferta.Situacao), situacao);
        return oferta;
    }

    private async Task EnsureSchemaAsync()
    {
        await using var context = CreateContext(
            _fixture.ConnectionStringFor("own_inventory", "own_inventory_dev_pw"));
        await context.Database.MigrateAsync();
    }

    private async Task ClearDataAsync()
    {
        await using var context = CreateContext();
        await context.Solicitacoes.ExecuteDeleteAsync();
        await context.Ofertas.ExecuteDeleteAsync();
    }

    private InventoryDbContext CreateContext() => CreateContext(
        _fixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"));

    private static InventoryDbContext CreateContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(connectionString, InventoryDbContext.Schema);
        return new InventoryDbContext(optionsBuilder.Options);
    }

    private static void SetProperty<T>(T target, string propertyName, object? value) =>
        typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(target, value);

    private sealed class ContextUnitOfWork(InventoryDbContext context) : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            context.SaveChangesAsync(cancellationToken);
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

    private sealed class StubCurrentUser(string userId, string displayName) : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public string? UserId => userId;

        public string? DisplayName => displayName;

        public IReadOnlyCollection<string> Permissions => ["estoque:validar"];

        public bool HasPermission(string permission) =>
            string.Equals(permission, "estoque:validar", StringComparison.Ordinal);
    }
}
