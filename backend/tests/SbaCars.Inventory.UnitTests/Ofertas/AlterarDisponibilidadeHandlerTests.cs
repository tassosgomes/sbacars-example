using Microsoft.Extensions.Logging.Abstractions;
using SbaCars.BuildingBlocks.Application;
using SbaCars.Inventory.Application.Integracao;
using SbaCars.Inventory.Application.Ofertas.AlterarDisponibilidade;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.UnitTests.Ofertas;

public sealed class AlterarDisponibilidadeHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 17, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_TransicaoPermitida_PersisteEDevolveEstadoAtualizado()
    {
        var oferta = Oferta.Criar(
            new Veiculo(TipoVeiculo.CarroUsado, placa: "ABC1D23"),
            new Autoria("operator-1", "Ana", Now),
            Now);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new AlterarDisponibilidadeCommandHandler(
            new FakeOfertaRepository(oferta),
            unitOfWork,
            new FakeIntegrationEventPublisher(),
            new FakeCurrentUser("operator-2", "Bruno"),
            new FixedClock(Now.AddMinutes(5)),
            NullLogger<AlterarDisponibilidadeCommandHandler>.Instance);

        var response = await handler.HandleAsync(
            new AlterarDisponibilidadeCommand
            {
                OfertaId = oferta.Id,
                NovoEstado = EstadoDisponibilidade.Reservado,
                Observacao = "Reserva temporária para atendimento.",
            },
            CancellationToken.None);

        unitOfWork.SaveCalls.Should().Be(1);
        response.Disponibilidade.Estado.Should().Be("reservado");
        response.Disponibilidade.TransicoesPermitidas.Should().Equal("disponivel", "vendido");
        oferta.Situacao.Should().Be(SituacaoOferta.EmPreparacao);
    }

    [Fact]
    public void Validator_ObservacaoAcimaDoLimite_RetornaErroDeRequisicao()
    {
        var result = new AlterarDisponibilidadeCommandValidator().Validate(
            new AlterarDisponibilidadeCommand
            {
                OfertaId = Guid.NewGuid(),
                NovoEstado = EstadoDisponibilidade.Reservado,
                Observacao = new string('x', 1001),
            });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == "Observacao");
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
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public void Adicionar(Oferta entity) => throw new NotSupportedException();

        public void Remover(Oferta entity) => throw new NotSupportedException();
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
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

        public IReadOnlyCollection<string> Permissions => ["estoque:gerenciar"];

        public bool HasPermission(string permission) =>
            string.Equals(permission, "estoque:gerenciar", StringComparison.Ordinal);
    }
}
