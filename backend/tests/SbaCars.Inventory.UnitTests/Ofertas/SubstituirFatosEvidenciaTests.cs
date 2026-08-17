using SbaCars.BuildingBlocks.Application;
using SbaCars.Inventory.Application.Integracao;
using SbaCars.Inventory.Application.Ofertas.SubstituirFatos;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.UnitTests.Ofertas;

public sealed class SubstituirFatosEvidenciaTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_ComEvidenciaId_PersisteReferenciaNoBlocoFato()
    {
        var oferta = CreateOferta();
        var evidencia = Evidencia.Criar(
            oferta.Id,
            "laudo.pdf",
            "application/pdf",
            1024,
            new Autoria("operator-1", "Ana", Now),
            Now);
        var evidenciaId = evidencia.Id;

        var handler = CreateHandler(oferta, [evidencia]);
        var command = new SubstituirFatosCommand
        {
            OfertaId = oferta.Id,
            Origem = new BlocoFatoInput
            {
                Descricao = "Origem com laudo",
                EvidenciaId = evidenciaId,
            },
            Condicao = new BlocoFatoInput { Fonte = "Laudo interno" },
            Historico = new BlocoFatoInput
            {
                Indisponivel = true,
                LimitacaoDeclarada = "Histórico indisponível.",
            },
        };

        var response = await handler.HandleAsync(command, CancellationToken.None);

        oferta.Fatos.Origem.EvidenciaId.Should().Be(evidenciaId);
        response.Fatos.Origem.Evidencia.Should().NotBeNull();
        response.Fatos.Origem.Evidencia!.EvidenciaId.Should().Be(evidenciaId);
    }

    [Fact]
    public async Task HandleAsync_EvidenciaDeOutraOferta_LancaEvidenciaNaoEncontradaException()
    {
        var oferta = CreateOferta();
        var evidencia = Evidencia.Criar(
            Guid.CreateVersion7(),
            "laudo.pdf",
            "application/pdf",
            1024,
            new Autoria("operator-1", "Ana", Now),
            Now);

        var handler = CreateHandler(oferta, [evidencia]);
        var command = new SubstituirFatosCommand
        {
            OfertaId = oferta.Id,
            Origem = new BlocoFatoInput
            {
                Descricao = "Origem",
                EvidenciaId = evidencia.Id,
            },
            Condicao = new BlocoFatoInput { Fonte = "Fonte" },
            Historico = new BlocoFatoInput
            {
                Indisponivel = true,
                LimitacaoDeclarada = "Sem histórico.",
            },
        };

        var act = () => handler.HandleAsync(command, CancellationToken.None);

        await act.Should().ThrowAsync<EvidenciaNaoEncontradaException>();
    }

    private static SubstituirFatosHandler CreateHandler(Oferta oferta, IReadOnlyList<Evidencia> evidencias) => new(
        new FakeOfertaRepository(oferta),
        new FakeEvidenciaRepository(evidencias),
        new FakeUnitOfWork(),
        new FakeIntegrationEventPublisher(),
        new StubCurrentUser("operator-1", "Ana"),
        new FixedClock(Now.AddMinutes(1)));

    private static Oferta CreateOferta() => Oferta.Criar(
        new Veiculo(TipoVeiculo.CarroUsado, placa: "ABC1D23"),
        new Autoria("operator-1", "Ana", Now),
        Now);

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

    private sealed class FakeEvidenciaRepository(IReadOnlyList<Evidencia> evidencias) : IEvidenciaRepository
    {
        public Task<Evidencia?> ObterAsync(
            Guid evidenciaId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(evidencias.SingleOrDefault(item => item.Id == evidenciaId));

        public Task<IReadOnlyList<Evidencia>> ObterVariosAsync(
            IEnumerable<Guid> evidenciaIds,
            CancellationToken cancellationToken = default)
        {
            var ids = evidenciaIds.ToHashSet();
            return Task.FromResult<IReadOnlyList<Evidencia>>(
                evidencias.Where(item => ids.Contains(item.Id)).ToArray());
        }

        public void Adicionar(Evidencia evidencia) => throw new NotSupportedException();
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

    private sealed class StubCurrentUser(string userId, string displayName) : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public string? UserId => userId;

        public string? DisplayName => displayName;

        public IReadOnlyCollection<string> Permissions => ["estoque:gerenciar"];

        public bool HasPermission(string permission) => Permissions.Contains(permission);
    }
}
