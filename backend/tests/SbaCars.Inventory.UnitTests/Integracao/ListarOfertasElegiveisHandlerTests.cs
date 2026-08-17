using SbaCars.BuildingBlocks.Application;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Integracao;
using SbaCars.Inventory.Application.Integracao.ListarOfertasElegiveis;

namespace SbaCars.Inventory.UnitTests.Integracao;

public sealed class ListarOfertasElegiveisHandlerTests
{
    [Fact]
    public async Task HandleAsync_DelegatesQueryAndCancellationToReadRepository()
    {
        var expected = new PagedResult<OfertaElegivelResponse>([], 1, 20, 0);
        var repository = new StubReadRepository(expected);
        var handler = new ListarOfertasElegiveisHandler(repository);
        var query = new ListarOfertasElegiveisQuery
        {
            Page = 2,
            PageSize = 10,
            AtualizadoApos = new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero),
        };
        using var cancellation = new CancellationTokenSource();

        var result = await handler.HandleAsync(query, cancellation.Token);

        result.Should().BeSameAs(expected);
        repository.Query.Should().BeSameAs(query);
        repository.CancellationToken.Should().Be(cancellation.Token);
    }

    private sealed class StubReadRepository(PagedResult<OfertaElegivelResponse> response)
        : IOfertaElegivelReadRepository
    {
        public ListarOfertasElegiveisQuery? Query { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task<PagedResult<OfertaElegivelResponse>> ListarAsync(
            ListarOfertasElegiveisQuery query,
            CancellationToken cancellationToken)
        {
            Query = query;
            CancellationToken = cancellationToken;
            return Task.FromResult(response);
        }
    }
}
