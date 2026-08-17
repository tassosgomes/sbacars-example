using SbaCars.BuildingBlocks.Application;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Solicitacoes;
using SbaCars.Inventory.Application.Solicitacoes.AbrirSolicitacao;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Domain.Solicitacoes;

namespace SbaCars.Inventory.UnitTests.Solicitacoes;

public sealed class AbrirSolicitacaoHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 17, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_EligibilityWithMissingCriteria_ThrowsAndDoesNotAddRequest()
    {
        var oferta = CreateOffer();
        var requests = new FakeSolicitacaoRepository();
        var handler = CreateHandler(oferta, requests);

        var act = () => handler.HandleAsync(
            new AbrirSolicitacaoCommand
            {
                OfertaId = oferta.Id,
                Tipo = TipoSolicitacao.Elegibilidade,
                Justificativa = "Cadastro pronto.",
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<CriteriosMinimosNaoAtendidosException>();
        requests.Added.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_PriceChange_KeepsCurrentPriceAndStoresProposedPrice()
    {
        var oferta = CreateOffer();
        oferta.DefinirPrecoInicial(8_790_000, "operator-1", "Ana Souza", Now);
        var requests = new FakeSolicitacaoRepository();
        var handler = CreateHandler(oferta, requests);

        var response = await handler.HandleAsync(
            new AbrirSolicitacaoCommand
            {
                OfertaId = oferta.Id,
                Tipo = TipoSolicitacao.Preco,
                NovoPrecoCentavos = 8_450_000,
                Justificativa = "Ajuste para o mercado.",
            },
            CancellationToken.None);

        oferta.PrecoOficial!.ValorCentavos.Should().Be(8_790_000);
        requests.Added.Should().ContainSingle();
        requests.Added.Single().NovoPrecoCentavos.Should().Be(8_450_000);
        response.Status.Should().Be("pendente");
        response.ValorVigente.Should().Be("R$ 87.900,00");
        response.ValorProposto.Should().Be("R$ 84.500,00");
    }

    [Fact]
    public async Task HandleAsync_DuplicatePendingRequest_ThrowsConflictBeforePreconditions()
    {
        var oferta = CreateOffer();
        var requests = new FakeSolicitacaoRepository { HasPending = true };
        var handler = CreateHandler(oferta, requests);

        var act = () => handler.HandleAsync(
            new AbrirSolicitacaoCommand
            {
                OfertaId = oferta.Id,
                Tipo = TipoSolicitacao.Retirada,
                Justificativa = "Retirar.",
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<SolicitacaoPendenteDuplicadaException>();
        requests.Added.Should().BeEmpty();
    }

    private static AbrirSolicitacaoHandler CreateHandler(
        Oferta oferta,
        FakeSolicitacaoRepository requests) => new(
        new FakeOfertaRepository(oferta),
        requests,
        new FakeUnitOfWork(),
        new FakeCurrentUser(),
        new FakeClock(Now),
        new CalculadoraDiasUteis());

    private static Oferta CreateOffer() => Oferta.Criar(
        new Veiculo(TipoVeiculo.CarroSeminovo, placa: "ABC1D23", marca: "Honda", modelo: "Civic"),
        new Autoria("operator-1", "Ana Souza", Now),
        Now);

    private sealed class FakeOfertaRepository : IOfertaRepository
    {
        private readonly Oferta _oferta;

        public FakeOfertaRepository(Oferta oferta)
        {
            _oferta = oferta;
        }

        public Task<Oferta?> ObterAsync(Guid ofertaId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Oferta?>(_oferta.Id == ofertaId ? _oferta : null);

        public Task<bool> ExistePlacaAtivaAsync(
            string placa,
            Guid? ignorarOfertaId = null,
            CancellationToken cancellationToken = default) => Task.FromResult(false);

        public void Adicionar(Oferta entity) => throw new NotSupportedException();

        public void Remover(Oferta entity) => throw new NotSupportedException();
    }

    private sealed class FakeSolicitacaoRepository : ISolicitacaoRepository
    {
        public bool HasPending { get; init; }

        public List<Solicitacao> Added { get; } = [];

        public Task<Solicitacao?> ObterAsync(
            Guid solicitacaoId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Solicitacao?>(Added.SingleOrDefault(item => item.Id == solicitacaoId));

        public Task<bool> ExistePendenteAsync(
            Guid ofertaId,
            TipoSolicitacao tipo,
            CancellationToken cancellationToken = default) => Task.FromResult(HasPending);

        public void Adicionar(Solicitacao solicitacao) => Added.Add(solicitacao);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FakeClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public string? UserId => "operator-1";

        public string? DisplayName => "Ana Souza";

        public IReadOnlyCollection<string> Permissions => [];

        public bool HasPermission(string permission) => false;
    }
}
