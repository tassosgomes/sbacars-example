using SbaCars.BuildingBlocks.Application;
using SbaCars.Inventory.Application.Integracao;
using SbaCars.Inventory.Application.Ofertas.CadastrarVeiculo;
using SbaCars.Inventory.Application.Ofertas.DefinirPrecoInicial;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.UnitTests.Ofertas;

public sealed class PublicacaoEventosOfertaHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CadastrarVeiculo_PublicaOfertaIncluidaAntesDePersistir()
    {
        var publisher = new RecordingIntegrationEventPublisher();
        var unitOfWork = new RecordingUnitOfWork(publisher, "incluida");
        var repository = new FakeOfertaRepository();
        var handler = new CadastrarVeiculoHandler(
            repository,
            unitOfWork,
            publisher,
            new FakeCurrentUser(),
            new FixedClock(Now));

        var response = await handler.HandleAsync(
            new CadastrarVeiculoCommand
            {
                TipoVeiculo = "carro-usado",
                Placa = "ABC1D23",
            },
            CancellationToken.None);

        repository.Added.Should().NotBeNull();
        publisher.Calls.Should().ContainSingle().Which.Should().Be(("incluida", response.OfertaId, Now));
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task DefinirPrecoInicial_PublicaOfertaAtualizadaAntesDePersistir()
    {
        var oferta = Oferta.Criar(
            new Veiculo(TipoVeiculo.CarroSeminovo, placa: "DEF4G56"),
            new Autoria("operator-1", "Ana", Now.AddMinutes(-5)),
            Now.AddMinutes(-5));
        var publisher = new RecordingIntegrationEventPublisher();
        var unitOfWork = new RecordingUnitOfWork(publisher, "atualizada");
        var handler = new DefinirPrecoInicialCommandHandler(
            new FakeOfertaRepository(oferta),
            unitOfWork,
            publisher,
            new FakeCurrentUser(),
            new FixedClock(Now));

        await handler.HandleAsync(
            new DefinirPrecoInicialCommand
            {
                OfertaId = oferta.Id,
                ValorCentavos = 8_790_000,
            },
            CancellationToken.None);

        publisher.Calls.Should().ContainSingle().Which.Should().Be(("atualizada", oferta.Id, Now));
        unitOfWork.SaveCalls.Should().Be(1);
    }

    private sealed class FakeOfertaRepository(Oferta? oferta = null) : IOfertaRepository
    {
        public Oferta? Added { get; private set; }

        public Task<Oferta?> ObterAsync(
            Guid ofertaId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(oferta?.Id == ofertaId ? oferta : null);

        public Task<bool> ExistePlacaAtivaAsync(
            string placa,
            Guid? ignorarOfertaId = null,
            CancellationToken cancellationToken = default) => Task.FromResult(false);

        public void Adicionar(Oferta entity) => Added = entity;

        public void Remover(Oferta entity) => throw new NotSupportedException();
    }

    private sealed class RecordingUnitOfWork(
        RecordingIntegrationEventPublisher publisher,
        string expectedEvent) : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            publisher.Calls.Should().Contain(call => call.Type == expectedEvent);
            SaveCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class RecordingIntegrationEventPublisher : IEstoqueIntegrationEventPublisher
    {
        public List<(string Type, Guid OfertaId, DateTimeOffset OccurredAt)> Calls { get; } = [];

        public Task PublishOfferIncludedAsync(
            Guid ofertaId,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(("incluida", ofertaId, occurredAt));
            return Task.CompletedTask;
        }

        public Task PublishOfferUpdatedAsync(
            Guid ofertaId,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(("atualizada", ofertaId, occurredAt));
            return Task.CompletedTask;
        }

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

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public string? UserId => "operator-1";

        public string? DisplayName => "Ana";

        public IReadOnlyCollection<string> Permissions => ["estoque:gerenciar"];

        public bool HasPermission(string permission) => Permissions.Contains(permission);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
