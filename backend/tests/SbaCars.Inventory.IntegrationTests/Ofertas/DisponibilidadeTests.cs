using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Integracao;
using SbaCars.Inventory.Application.Ofertas.AlterarDisponibilidade;
using SbaCars.Inventory.Application.Solicitacoes.AprovarSolicitacao;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Domain.Solicitacoes;
using SbaCars.Inventory.Infrastructure;
using SbaCars.Inventory.Infrastructure.Ofertas;
using SbaCars.Inventory.Infrastructure.Solicitacoes;
using SbaCars.TestKit;

using InventoryTestRepositories = SbaCars.Inventory.IntegrationTests.InventoryTestRepositories;

namespace SbaCars.Inventory.IntegrationTests.Ofertas;

[Collection(InventoryPostgresCollection.Name)]
public sealed class DisponibilidadeTests
{
    private readonly SbaCarsPostgresFixture _fixture;

    public DisponibilidadeTests(SbaCarsPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AlterarDisponibilidade_TransicoesDiretasPermitidas_PersisteEstadoDesdeETransicoes()
    {
        await EnsureSchemaAsync();
        await ClearDataAsync();

        var now = new DateTimeOffset(2026, 8, 17, 9, 0, 0, TimeSpan.Zero);
        var oferta = CreateOffer(now, "ABC1D23");
        await AddOfferAsync(oferta);

        await AlterAsync(oferta.Id, EstadoDisponibilidade.Reservado, now.AddMinutes(1));
        await AlterAsync(oferta.Id, EstadoDisponibilidade.Disponivel, now.AddMinutes(2));
        await AlterAsync(oferta.Id, EstadoDisponibilidade.Vendido, now.AddMinutes(3));

        await using var read = CreateContext();
        var persisted = await read.Ofertas.AsNoTracking().SingleAsync(item => item.Id == oferta.Id);

        persisted.Disponibilidade.Estado.Should().Be(EstadoDisponibilidade.Vendido);
        persisted.Disponibilidade.Desde.Should().Be(now.AddMinutes(3));
        persisted.Disponibilidade.EstadoConhecido.Should().BeTrue();
        persisted.Disponibilidade.TransicoesPermitidas.Should().BeEmpty();
    }

    [Fact]
    public async Task AlterarDisponibilidade_VendidoParaDisponivelDireto_RejeitaEConservaEstado()
    {
        await EnsureSchemaAsync();
        await ClearDataAsync();

        var now = new DateTimeOffset(2026, 8, 17, 9, 0, 0, TimeSpan.Zero);
        var oferta = CreateOffer(now, "DEF4G56");
        oferta.AlterarDisponibilidade(
            EstadoDisponibilidade.Vendido,
            null,
            new Autoria("operator-1", "Ana", now),
            now.AddMinutes(1));
        await AddOfferAsync(oferta);

        await using (var context = CreateContext())
        {
            var handler = CreateAvailabilityHandler(context, now.AddMinutes(2));
            var act = () => handler.HandleAsync(
                new AlterarDisponibilidadeCommand
                {
                    OfertaId = oferta.Id,
                    NovoEstado = EstadoDisponibilidade.Disponivel,
                },
                CancellationToken.None);

            await act.Should().ThrowAsync<TransicaoInvalidaException>();
        }

        await using var read = CreateContext();
        var persisted = await read.Ofertas.AsNoTracking().SingleAsync(item => item.Id == oferta.Id);
        persisted.Disponibilidade.Estado.Should().Be(EstadoDisponibilidade.Vendido);
        persisted.Disponibilidade.Desde.Should().Be(now.AddMinutes(1));
    }

    [Fact]
    public async Task AprovarReversaoVenda_AlteraDisponibilidadeSemAlterarSituacao()
    {
        await EnsureSchemaAsync();
        await ClearDataAsync();

        var now = new DateTimeOffset(2026, 8, 17, 9, 0, 0, TimeSpan.Zero);
        var oferta = CreateOffer(now, "GHI7H89");
        oferta.AlterarDisponibilidade(
            EstadoDisponibilidade.Vendido,
            null,
            new Autoria("operator-1", "Ana", now),
            now.AddMinutes(1));
        var solicitacao = Solicitacao.Abrir(
            oferta.Id,
            TipoSolicitacao.ReversaoVenda,
            null,
            "Venda cancelada pelo operador.",
            new Autoria("operator-1", "Ana", now),
            now.AddMinutes(2));

        await using (var setup = CreateContext())
        {
            setup.Ofertas.Add(oferta);
            setup.Solicitacoes.Add(solicitacao);
            await setup.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var handler = new AprovarSolicitacaoHandler(
                new SolicitacaoRepository(context, new CalculadoraDiasUteis()),
                InventoryTestRepositories.CreateOfertaRepository(context),
                new ContextUnitOfWork(context),
                new FakeIntegrationEventPublisher(),
                new StubCurrentUser("validator-1", "Bruno"),
                new FixedClock(now.AddHours(1)),
                new CalculadoraDiasUteis(),
                NullLogger<AprovarSolicitacaoHandler>.Instance);

            await handler.HandleAsync(
                new AprovarSolicitacaoCommand { SolicitacaoId = solicitacao.Id },
                CancellationToken.None);
        }

        await using var read = CreateContext();
        var persistedOffer = await read.Ofertas.AsNoTracking().SingleAsync(item => item.Id == oferta.Id);
        var persistedRequest = await read.Solicitacoes.AsNoTracking().SingleAsync(item => item.Id == solicitacao.Id);

        persistedOffer.Disponibilidade.Estado.Should().Be(EstadoDisponibilidade.Disponivel);
        persistedOffer.Disponibilidade.TransicoesPermitidas.Should().BeEquivalentTo(
            [EstadoDisponibilidade.Reservado, EstadoDisponibilidade.Vendido]);
        persistedOffer.Situacao.Should().Be(SituacaoOferta.EmPreparacao);
        persistedRequest.Status.Should().Be(StatusSolicitacao.Aprovada);
    }

    [Fact]
    public async Task RetirarOferta_DisponibilidadePermaneceIndependente()
    {
        await EnsureSchemaAsync();
        await ClearDataAsync();

        var now = new DateTimeOffset(2026, 8, 17, 9, 0, 0, TimeSpan.Zero);
        var autoria = new Autoria("operator-1", "Ana", now);
        var oferta = CreateOffer(now, "JKL0M12");
        oferta.AlterarDisponibilidade(
            EstadoDisponibilidade.Reservado,
            null,
            autoria,
            now.AddMinutes(1));
        oferta.Retirar(autoria, now.AddMinutes(2));
        await AddOfferAsync(oferta);

        await using var read = CreateContext();
        var persisted = await read.Ofertas.AsNoTracking().SingleAsync(item => item.Id == oferta.Id);
        persisted.Situacao.Should().Be(SituacaoOferta.Retirada);
        persisted.Disponibilidade.Estado.Should().Be(EstadoDisponibilidade.Reservado);
    }

    private async Task AlterAsync(
        Guid ofertaId,
        EstadoDisponibilidade novoEstado,
        DateTimeOffset now)
    {
        await using var context = CreateContext();
        var handler = CreateAvailabilityHandler(context, now);

        await handler.HandleAsync(
            new AlterarDisponibilidadeCommand
            {
                OfertaId = ofertaId,
                NovoEstado = novoEstado,
                Observacao = "Atualização explícita da operação.",
            },
            CancellationToken.None);
    }

    private AlterarDisponibilidadeCommandHandler CreateAvailabilityHandler(
        InventoryDbContext context,
        DateTimeOffset now) => new(
        InventoryTestRepositories.CreateOfertaRepository(context),
        new ContextUnitOfWork(context),
        new FakeIntegrationEventPublisher(),
        new StubCurrentUser("operator-2", "Bruno"),
        new FixedClock(now),
        NullLogger<AlterarDisponibilidadeCommandHandler>.Instance);

    private async Task AddOfferAsync(Oferta oferta)
    {
        await using var context = CreateContext();
        context.Ofertas.Add(oferta);
        await context.SaveChangesAsync();
    }

    private static Oferta CreateOffer(DateTimeOffset now, string plate) => Oferta.Criar(
        new Veiculo(TipoVeiculo.CarroSeminovo, placa: plate),
        new Autoria("operator-1", "Ana", now),
        now);

    private async Task EnsureSchemaAsync()
    {
        await using var context = CreateContext(
            _fixture.ConnectionStringFor("own_inventory", "own_inventory_dev_pw"));
        await context.Database.MigrateAsync();
    }

    private async Task ClearDataAsync()
    {
        await using var context = CreateContext();
        await context.Evidencias.ExecuteDeleteAsync();
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

        public IReadOnlyCollection<string> Permissions => ["estoque:gerenciar", "estoque:validar"];

        public bool HasPermission(string permission) => Permissions.Contains(permission);
    }
}
