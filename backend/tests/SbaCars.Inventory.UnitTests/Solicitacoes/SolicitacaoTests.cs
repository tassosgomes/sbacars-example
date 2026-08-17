using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Domain.Solicitacoes;

namespace SbaCars.Inventory.UnitTests.Solicitacoes;

public sealed class SolicitacaoTests
{
    [Fact]
    public void Abrir_PriceRequest_PreservesPendingStatusAndProposedPrice()
    {
        var abertaEm = new DateTimeOffset(2026, 8, 17, 10, 0, 0, TimeSpan.Zero);
        var autoria = new Autoria("operator-1", "Ana Souza", abertaEm);
        var ofertaId = Guid.NewGuid();

        var solicitacao = Solicitacao.Abrir(
            ofertaId,
            TipoSolicitacao.Preco,
            8_450_000,
            "Ajuste de mercado.",
            autoria,
            abertaEm);

        solicitacao.OfertaId.Should().Be(ofertaId);
        solicitacao.Tipo.Should().Be(TipoSolicitacao.Preco);
        solicitacao.Status.Should().Be(StatusSolicitacao.Pendente);
        solicitacao.NovoPrecoCentavos.Should().Be(8_450_000);
        solicitacao.AbertaPor.Should().Be(autoria);
        solicitacao.AbertaEm.Should().Be(abertaEm);
    }

    [Fact]
    public void Abrir_NonPriceRequestWithPrice_Throws()
    {
        var act = () => Solicitacao.Abrir(
            Guid.NewGuid(),
            TipoSolicitacao.Retirada,
            100,
            "Retirar.",
            new Autoria("operator-1", "Ana", DateTimeOffset.UtcNow),
            DateTimeOffset.UtcNow);

        act.Should().Throw<CampoPrecoSolicitacaoNaoPermitidoException>();
    }

    [Fact]
    public void Abrir_PriceRequestWithoutPrice_Throws()
    {
        var act = () => Solicitacao.Abrir(
            Guid.NewGuid(),
            TipoSolicitacao.Preco,
            null,
            "Ajustar.",
            new Autoria("operator-1", "Ana", DateTimeOffset.UtcNow),
            DateTimeOffset.UtcNow);

        act.Should().Throw<PrecoSolicitacaoObrigatorioException>();
    }
}
