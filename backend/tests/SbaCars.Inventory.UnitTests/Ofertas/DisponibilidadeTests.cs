using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.UnitTests.Ofertas;

public sealed class DisponibilidadeTests
{
    private static readonly DateTimeOffset InitialInstant =
        new(2026, 8, 17, 9, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(EstadoDisponibilidade.Disponivel, EstadoDisponibilidade.Reservado)]
    [InlineData(EstadoDisponibilidade.Disponivel, EstadoDisponibilidade.Vendido)]
    [InlineData(EstadoDisponibilidade.Reservado, EstadoDisponibilidade.Disponivel)]
    [InlineData(EstadoDisponibilidade.Reservado, EstadoDisponibilidade.Vendido)]
    public void Alterar_TransicaoDiretaPermitida_AtualizaEstadoDesdeEAutoria(
        EstadoDisponibilidade estadoInicial,
        EstadoDisponibilidade novoEstado)
    {
        var oferta = CreateOffer();
        var operatorAuthorship = new Autoria("operator-2", "Bruno", InitialInstant.AddMinutes(1));

        MoveTo(oferta, estadoInicial, operatorAuthorship, InitialInstant.AddMinutes(1));
        var transitionInstant = InitialInstant.AddMinutes(2);

        oferta.AlterarDisponibilidade(
            novoEstado,
            "Contexto operacional não deve aparecer no log.",
            operatorAuthorship,
            transitionInstant);

        oferta.Disponibilidade.Estado.Should().Be(novoEstado);
        oferta.Disponibilidade.Desde.Should().Be(transitionInstant);
        oferta.Disponibilidade.AlteradaPor.Should().Be(operatorAuthorship);
        oferta.Disponibilidade.EstadoConhecido.Should().BeTrue();
        oferta.Disponibilidade.TransicoesPermitidas.Should().BeEquivalentTo(
            ExpectedTransitions(novoEstado));
    }

    [Theory]
    [InlineData(EstadoDisponibilidade.Disponivel, EstadoDisponibilidade.Disponivel)]
    [InlineData(EstadoDisponibilidade.Reservado, EstadoDisponibilidade.Reservado)]
    [InlineData(EstadoDisponibilidade.Vendido, EstadoDisponibilidade.Disponivel)]
    [InlineData(EstadoDisponibilidade.Vendido, EstadoDisponibilidade.Vendido)]
    public void Alterar_TransicaoDiretaInvalida_LancaExcecaoSemMutar(
        EstadoDisponibilidade estadoInicial,
        EstadoDisponibilidade novoEstado)
    {
        var oferta = CreateOffer();
        var operatorAuthorship = new Autoria("operator-2", "Bruno", InitialInstant.AddMinutes(1));
        MoveTo(oferta, estadoInicial, operatorAuthorship, InitialInstant.AddMinutes(1));
        var estadoAntes = oferta.Disponibilidade.Estado;
        var desdeAntes = oferta.Disponibilidade.Desde;
        var autoriaAntes = oferta.Disponibilidade.AlteradaPor;

        var act = () => oferta.AlterarDisponibilidade(
            novoEstado,
            null,
            operatorAuthorship,
            InitialInstant.AddMinutes(2));

        var exception = act.Should().Throw<TransicaoInvalidaException>().Which;

        exception.EstadoAtual.Should().Be(estadoInicial);
        exception.NovoEstado.Should().Be(novoEstado);
        oferta.Disponibilidade.Estado.Should().Be(estadoAntes);
        oferta.Disponibilidade.Desde.Should().Be(desdeAntes);
        oferta.Disponibilidade.AlteradaPor.Should().Be(autoriaAntes);
    }

    [Fact]
    public void Alterar_ReservaNaoExpiraSemAcaoExplicita()
    {
        var oferta = CreateOffer();
        var operatorAuthorship = new Autoria("operator-2", "Bruno", InitialInstant.AddMinutes(1));
        var reservedAt = InitialInstant.AddMinutes(1);

        oferta.AlterarDisponibilidade(
            EstadoDisponibilidade.Reservado,
            null,
            operatorAuthorship,
            reservedAt);

        oferta.Disponibilidade.Estado.Should().Be(EstadoDisponibilidade.Reservado);
        oferta.Disponibilidade.Desde.Should().Be(reservedAt);
        oferta.Disponibilidade.Estado.Should().NotBe(EstadoDisponibilidade.Disponivel);
    }

    [Fact]
    public void AlterarDisponibilidade_NaoAlteraSituacaoDaOferta()
    {
        var oferta = CreateOffer();
        var situacaoAntes = oferta.Situacao;
        var autoria = new Autoria("operator-2", "Bruno", InitialInstant.AddMinutes(1));

        oferta.AlterarDisponibilidade(
            EstadoDisponibilidade.Reservado,
            null,
            autoria,
            InitialInstant.AddMinutes(1));

        oferta.Situacao.Should().Be(situacaoAntes);
    }

    [Fact]
    public void Retirar_NaoAlteraDisponibilidadeDaOferta()
    {
        var oferta = CreateOffer();
        var autoria = new Autoria("operator-2", "Bruno", InitialInstant.AddMinutes(1));
        oferta.AlterarDisponibilidade(
            EstadoDisponibilidade.Reservado,
            null,
            autoria,
            InitialInstant.AddMinutes(1));
        var disponibilidadeAntes = oferta.Disponibilidade;

        oferta.Retirar(autoria, InitialInstant.AddMinutes(2));

        oferta.Situacao.Should().Be(SituacaoOferta.Retirada);
        oferta.Disponibilidade.Should().BeSameAs(disponibilidadeAntes);
        oferta.Disponibilidade.Estado.Should().Be(EstadoDisponibilidade.Reservado);
    }

    private static Oferta CreateOffer() => Oferta.Criar(
        new Veiculo(TipoVeiculo.CarroSeminovo, placa: "ABC1D23"),
        new Autoria("operator-1", "Ana", InitialInstant),
        InitialInstant);

    private static void MoveTo(
        Oferta oferta,
        EstadoDisponibilidade estado,
        Autoria autoria,
        DateTimeOffset instant)
    {
        if (estado == EstadoDisponibilidade.Disponivel)
        {
            return;
        }

        oferta.AlterarDisponibilidade(EstadoDisponibilidade.Reservado, null, autoria, instant);
        if (estado == EstadoDisponibilidade.Vendido)
        {
            oferta.AlterarDisponibilidade(EstadoDisponibilidade.Vendido, null, autoria, instant.AddMinutes(1));
        }
    }

    private static IReadOnlyCollection<EstadoDisponibilidade> ExpectedTransitions(
        EstadoDisponibilidade estado) => estado switch
        {
            EstadoDisponibilidade.Disponivel =>
                [EstadoDisponibilidade.Reservado, EstadoDisponibilidade.Vendido],
            EstadoDisponibilidade.Reservado =>
                [EstadoDisponibilidade.Disponivel, EstadoDisponibilidade.Vendido],
            EstadoDisponibilidade.Vendido => [],
            _ => [],
        };
}
