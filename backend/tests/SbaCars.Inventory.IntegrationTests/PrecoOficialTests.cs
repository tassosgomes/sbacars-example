using Microsoft.EntityFrameworkCore;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Inventory.Application.Integracao;
using SbaCars.Inventory.Application.Ofertas.DefinirPrecoInicial;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Infrastructure;
using SbaCars.Inventory.Infrastructure.Ofertas;
using SbaCars.TestKit;

namespace SbaCars.Inventory.IntegrationTests;

[Collection(InventoryPostgresCollection.Name)]
public sealed class PrecoOficialTests
{
    private readonly SbaCarsPostgresFixture _fixture;

    public PrecoOficialTests(SbaCarsPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DefinirPrecoInicial_PersistePrimeiroValorESegundoComandoRetorna409SemSobrescrever()
    {
        await EnsureSchemaAsync();
        await ClearOffersAsync();

        var primeiroInstante = new DateTimeOffset(2026, 8, 16, 14, 22, 5, TimeSpan.Zero);
        var oferta = Oferta.Criar(
            new Veiculo(
                TipoVeiculo.CarroSeminovo,
                placa: "ABC1D23",
                marca: "Honda",
                modelo: "Civic"),
            new Autoria("creator-1", "Criador", primeiroInstante),
            primeiroInstante);

        await using (var setupContext = CreateContext())
        {
            setupContext.Ofertas.Add(oferta);
            await setupContext.SaveChangesAsync();
        }

        await using (var firstContext = CreateContext())
        {
            var handler = CreateHandler(firstContext, "operator-42", "Ana Souza", primeiroInstante);

            var response = await handler.HandleAsync(
                new DefinirPrecoInicialCommand
                {
                    OfertaId = oferta.Id,
                    ValorCentavos = 8_790_000,
                },
                CancellationToken.None);

            response.PrecoOficial.Should().NotBeNull();
            response.PrecoOficial!.ValorCentavos.Should().Be(8_790_000);
            response.PrecoOficial.Moeda.Should().Be("BRL");
            response.PrecoOficial.DefinidoPor.Should().NotBeNull();
            response.PrecoOficial.DefinidoPor!.UsuarioId.Should().Be("operator-42");
            response.PrecoOficial.DefinidoPor.Nome.Should().Be("Ana Souza");
            response.PrecoOficial.DefinidoPor.Em.Should().Be(primeiroInstante);
            response.Elegibilidade.Criterios
                .Single(criterio => criterio.Codigo == "preco-oficial")
                .Atendido.Should().BeTrue();
            response.Situacao.Should().Be("em-preparacao");
            response.Disponibilidade.Estado.Should().Be("disponivel");
        }

        await using (var secondContext = CreateContext())
        {
            var segundoInstante = primeiroInstante.AddMinutes(5);
            var handler = CreateHandler(secondContext, "operator-43", "Bruno Lima", segundoInstante);

            var act = () => handler.HandleAsync(
                new DefinirPrecoInicialCommand
                {
                    OfertaId = oferta.Id,
                    ValorCentavos = 8_450_000,
                },
                CancellationToken.None);

            await act.Should().ThrowAsync<PrecoJaDefinidoException>();
        }

        await using var readContext = CreateContext();
        var persisted = await InventoryTestRepositories.CreateOfertaRepository(readContext)
            .ObterDetalheAsync(oferta.Id, CancellationToken.None);

        persisted.Should().NotBeNull();
        persisted!.PrecoOficial.Should().NotBeNull();
        persisted.PrecoOficial!.ValorCentavos.Should().Be(8_790_000);
        persisted.PrecoOficial.DefinidoPor!.UsuarioId.Should().Be("operator-42");
        persisted.PrecoOficial.DefinidoPor.Nome.Should().Be("Ana Souza");
        persisted.PrecoOficial.DefinidoPor.Em.Should().Be(primeiroInstante);
    }

    private static DefinirPrecoInicialCommandHandler CreateHandler(
        InventoryDbContext context,
        string userId,
        string displayName,
        DateTimeOffset now) => new(
        InventoryTestRepositories.CreateOfertaRepository(context),
        new ContextUnitOfWork(context),
        new NoOpIntegrationEventPublisher(),
        new StubCurrentUser(userId, displayName),
        new FixedClock(now));

    private async Task EnsureSchemaAsync()
    {
        await using var context = CreateContext(
            _fixture.ConnectionStringFor("own_inventory", "own_inventory_dev_pw"));
        await context.Database.MigrateAsync();
    }

    private async Task ClearOffersAsync()
    {
        await using var context = CreateContext();
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

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class NoOpIntegrationEventPublisher : IEstoqueIntegrationEventPublisher
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

    private sealed class StubCurrentUser(string userId, string displayName) : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public string? UserId => userId;

        public string? DisplayName => displayName;

        public IReadOnlyCollection<string> Permissions => ["estoque:gerenciar"];

        public bool HasPermission(string permission) =>
            string.Equals(permission, "estoque:gerenciar", StringComparison.Ordinal);
    }
}
