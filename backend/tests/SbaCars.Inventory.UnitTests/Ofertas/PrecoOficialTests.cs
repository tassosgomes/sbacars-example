using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.UnitTests.Ofertas;

public sealed class PrecoOficialTests
{
    [Fact]
    public void DefinirPrecoInicial_OfertaSemPreco_PreenchePrecoEAtendeCm4SemMudarEstado()
    {
        var instante = new DateTimeOffset(2026, 8, 16, 14, 22, 5, TimeSpan.Zero);
        var oferta = CriarOfertaSemPreco(instante);

        oferta.DefinirPrecoInicial(8_790_000, "operator-42", "Ana Souza", instante);

        oferta.PrecoOficial.Should().NotBeNull();
        oferta.PrecoOficial!.ValorCentavos.Should().Be(8_790_000);
        oferta.PrecoOficial.Moeda.Should().Be("BRL");
        oferta.PrecoOficial.DefinidoPor.UsuarioId.Should().Be("operator-42");
        oferta.PrecoOficial.DefinidoPor.Nome.Should().Be("Ana Souza");
        oferta.PrecoOficial.DefinidoPor.Em.Should().Be(instante);
        oferta.AvaliarCriteriosMinimos().Should().NotContain(CodigoCriterio.PrecoOficial);
        oferta.Situacao.Should().Be(SituacaoOferta.EmPreparacao);
        oferta.Disponibilidade.Estado.Should().Be(EstadoDisponibilidade.Disponivel);
    }

    [Fact]
    public void DefinirPrecoInicial_OfertaJaPrecificada_LancaSemSobrescreverValorOuAutoria()
    {
        var primeiroInstante = new DateTimeOffset(2026, 8, 16, 14, 22, 5, TimeSpan.Zero);
        var segundoInstante = primeiroInstante.AddMinutes(5);
        var oferta = CriarOfertaSemPreco(primeiroInstante);
        oferta.DefinirPrecoInicial(8_790_000, "operator-42", "Ana Souza", primeiroInstante);

        var act = () => oferta.DefinirPrecoInicial(
            8_450_000,
            "operator-43",
            "Bruno Lima",
            segundoInstante);

        act.Should().Throw<PrecoJaDefinidoException>();
        oferta.PrecoOficial!.ValorCentavos.Should().Be(8_790_000);
        oferta.PrecoOficial.DefinidoPor.UsuarioId.Should().Be("operator-42");
        oferta.PrecoOficial.DefinidoPor.Nome.Should().Be("Ana Souza");
        oferta.PrecoOficial.DefinidoPor.Em.Should().Be(primeiroInstante);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DefinirPrecoInicial_ValorNaoPositivo_LancaAntesDeCriarPreco(long valorCentavos)
    {
        var instante = new DateTimeOffset(2026, 8, 16, 14, 22, 5, TimeSpan.Zero);
        var oferta = CriarOfertaSemPreco(instante);

        var act = () => oferta.DefinirPrecoInicial(
            valorCentavos,
            "operator-42",
            "Ana Souza",
            instante);

        act.Should().Throw<ArgumentOutOfRangeException>();
        oferta.PrecoOficial.Should().BeNull();
    }

    private static Oferta CriarOfertaSemPreco(DateTimeOffset instante) => Oferta.Criar(
        new Veiculo(
            TipoVeiculo.CarroSeminovo,
            placa: "ABC1D23",
            marca: "Honda",
            modelo: "Civic"),
        new Autoria("operator-1", "Operador", instante),
        instante);
}
