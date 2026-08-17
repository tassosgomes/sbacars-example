using SbaCars.BuildingBlocks.Application;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Ofertas.ListarOfertas;
using SbaCars.Inventory.Application.Ofertas.ObterOferta;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.UnitTests.Ofertas;

public sealed class ObterOfertaTests
{
    [Fact]
    public async Task HandleAsync_PartialOfferWithPlate_ReturnsChecklistFromDomainEvaluation()
    {
        var now = new DateTimeOffset(2026, 8, 16, 12, 30, 0, TimeSpan.Zero);
        var oferta = Oferta.Criar(
            new Veiculo(TipoVeiculo.CarroSeminovo, placa: "ABC1D23"),
            new Autoria("operator-42", "Ana Souza", now),
            now);
        var expectedId = oferta.Id;
        var repository = new StubReadRepository(OfertaResponseMapper.ToDetalhe(oferta));
        var handler = new ObterOfertaHandler(repository);

        var response = await handler.HandleAsync(
            new ObterOfertaQuery(expectedId),
            CancellationToken.None);

        response.OfertaId.Should().Be(expectedId);
        response.Elegibilidade.Total.Should().Be(6);
        response.Elegibilidade.Atendidos.Should().Be(1);
        response.Elegibilidade.PodeSolicitarElegibilidade.Should().BeFalse();
        response.Elegibilidade.Criterios.Select(criterio => criterio.Codigo)
            .Should().Equal(
                "identificacao",
                "dados-basicos",
                "localizacao",
                "preco-oficial",
                "disponibilidade",
                "transparencia-fatos");
        response.Elegibilidade.Criterios
            .Where(criterio => !criterio.Atendido)
            .Should().OnlyContain(criterio => !string.IsNullOrWhiteSpace(criterio.Pendencia));
        response.Disponibilidade.TransicoesPermitidas
            .Should().Equal("reservado", "vendido");
        response.Fatos.Origem.AtendeTransparencia.Should().BeFalse();
        response.Fatos.Condicao.AtendeTransparencia.Should().BeFalse();
        response.Fatos.Historico.AtendeTransparencia.Should().BeFalse();
        response.Pendencias.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_UnknownOffer_ThrowsNotFoundDomainException()
    {
        var ofertaId = Guid.NewGuid();
        var handler = new ObterOfertaHandler(new StubReadRepository(null));

        var act = () => handler.HandleAsync(
            new ObterOfertaQuery(ofertaId),
            CancellationToken.None);

        await act.Should().ThrowAsync<OfertaNaoEncontradaException>();
    }

    private sealed class StubReadRepository(OfertaDetalheResponse? response) : IOfertaReadRepository
    {
        public Task<OfertaDetalheResponse?> ObterDetalheAsync(
            Guid ofertaId,
            CancellationToken cancellationToken) => Task.FromResult(response);

        public Task<PagedResult<OfertaResumoResponse>> ListarAsync(
            ListarOfertasQuery query,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("List operation is not part of this test.");
    }
}
