using System.Reflection;

using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.UnitTests.Ofertas;

public sealed class OfertaTests
{
    [Fact]
    public void Criar_ComDadosParciais_MantemOfertaEmPreparacaoERegistraAutoria()
    {
        var now = new DateTimeOffset(2026, 8, 16, 12, 30, 0, TimeSpan.Zero);
        var autoria = new Autoria("operator-42", "Ana Souza", now);
        var veiculo = new Veiculo(TipoVeiculo.CarroSeminovo, placa: "ABC-1D23", marca: "Honda");

        var oferta = Oferta.Criar(veiculo, autoria, now);

        oferta.Situacao.Should().Be(SituacaoOferta.EmPreparacao);
        oferta.Veiculo.Placa.Should().Be("ABC1D23");
        oferta.CriadaPor.Should().Be(autoria);
        oferta.AtualizadaPor.Should().Be(autoria);
        oferta.Disponibilidade.Estado.Should().Be(EstadoDisponibilidade.Disponivel);
        oferta.AvaliarCriteriosMinimos().Should().BeEquivalentTo(
        [
            CodigoCriterio.DadosBasicos,
            CodigoCriterio.Localizacao,
            CodigoCriterio.PrecoOficial,
            CodigoCriterio.Disponibilidade,
            CodigoCriterio.TransparenciaFatos,
        ], options => options.WithStrictOrdering());
    }

    [Fact]
    public void Criar_ComTipoForaDaCategoriaPermitida_LancaExcecaoDeDominio()
    {
        var veiculo = new Veiculo((TipoVeiculo)999);
        var autoria = new Autoria("operator-42", "Ana Souza", DateTimeOffset.UtcNow);

        var act = () => Oferta.Criar(veiculo, autoria, DateTimeOffset.UtcNow);

        act.Should().Throw<TipoVeiculoNaoPermitidoException>();
    }

    [Fact]
    public void BlocoFatoIndisponivel_ExigeLimitacaoDeclarada()
    {
        var act = () => new BlocoFato(BlocoFatoTipo.Condicao, indisponivel: true);

        act.Should().Throw<LimitacaoNaoDeclaradaException>();
    }

    [Fact]
    public void BlocoFatoIndisponivelComLimitacao_AtendeTransparencia()
    {
        var fato = new BlocoFato(
            BlocoFatoTipo.Historico,
            indisponivel: true,
            limitacaoDeclarada: "Não localizado no acervo físico.");

        fato.AtendeTransparencia.Should().BeTrue();
    }

    [Fact]
    public void BlocoFatoIndisponivel_IgnoresConteudoEnviadoComALimitacao()
    {
        var fato = new BlocoFato(
            BlocoFatoTipo.Condicao,
            indisponivel: true,
            descricao: "não deve ser mantida",
            fonte: "não deve ser mantida",
            limitacaoDeclarada: "A base consultada não retornou dados.");

        fato.Descricao.Should().BeNull();
        fato.Fonte.Should().BeNull();
        fato.LimitacaoDeclarada.Should().Be("A base consultada não retornou dados.");
        fato.AtendeTransparencia.Should().BeTrue();
    }

    [Fact]
    public void SubstituirFatos_ComTresBlocosPreenchidos_AtendeTransparenciaECm6()
    {
        var now = new DateTimeOffset(2026, 8, 16, 12, 30, 0, TimeSpan.Zero);
        var autoria = new Autoria("operator-42", "Ana Souza", now);
        var oferta = Oferta.Criar(
            new Veiculo(TipoVeiculo.CarroSeminovo, placa: "ABC1D23"),
            autoria,
            now);
        var fatos = FatosConhecidos.Criar(
            new BlocoFato(BlocoFatoTipo.Origem, descricao: "Frota corporativa", atualizadoPor: autoria),
            new BlocoFato(BlocoFatoTipo.Condicao, fonte: "Laudo interno", atualizadoPor: autoria),
            new BlocoFato(
                BlocoFatoTipo.Historico,
                indisponivel: true,
                limitacaoDeclarada: "Histórico indisponível nas bases consultadas.",
                atualizadoPor: autoria));

        oferta.SubstituirFatos(fatos, autoria, now, confirmaSuspensao: false);

        oferta.Fatos.Should().BeSameAs(fatos);
        oferta.Fatos.AtendeTransparencia.Should().BeTrue();
        oferta.AvaliarCriteriosMinimos().Should().NotContain(CodigoCriterio.TransparenciaFatos);
    }

    [Fact]
    public void SubstituirFatos_OfertaElegivelSemTransparenciaSemConfirmacao_NaoMutaENaoSuspende()
    {
        var now = new DateTimeOffset(2026, 8, 16, 12, 30, 0, TimeSpan.Zero);
        var oferta = CriarOfertaElegivel(now);
        var fatosOriginais = oferta.Fatos;
        var atualizadoEmOriginal = oferta.AtualizadoEm;
        var fatosVazios = FatosConhecidos.Vazios();

        var act = () => oferta.SubstituirFatos(
            fatosVazios,
            new Autoria("operator-43", "Bruno Lima", now.AddMinutes(1)),
            now.AddMinutes(1),
            confirmaSuspensao: false);

        var exception = act.Should().Throw<SuspensaoNaoConfirmadaException>().Which;

        exception.CriteriosAfetados.Should().Equal(CodigoCriterio.TransparenciaFatos);
        oferta.Fatos.Should().BeSameAs(fatosOriginais);
        oferta.AtualizadoEm.Should().Be(atualizadoEmOriginal);
        oferta.Situacao.Should().Be(SituacaoOferta.Elegivel);
        oferta.SuspensaEm.Should().BeNull();
    }

    [Fact]
    public void SubstituirFatos_OfertaElegivelComConfirmacao_AplicaESuspende()
    {
        var now = new DateTimeOffset(2026, 8, 16, 12, 30, 0, TimeSpan.Zero);
        var oferta = CriarOfertaElegivel(now);
        var fatosVazios = FatosConhecidos.Vazios();
        var atualizadaEm = now.AddMinutes(1);
        var autoria = new Autoria("operator-43", "Bruno Lima", atualizadaEm);

        oferta.SubstituirFatos(fatosVazios, autoria, atualizadaEm, confirmaSuspensao: true);

        oferta.Fatos.Should().BeSameAs(fatosVazios);
        oferta.Situacao.Should().Be(SituacaoOferta.Suspensa);
        oferta.SuspensaEm.Should().Be(atualizadaEm);
        oferta.MotivoSuspensao.Should().Contain("transparencia-fatos");
        oferta.AtualizadaPor.Should().Be(autoria);
    }

    [Fact]
    public void ParseTipoVeiculo_RejeitaCategoriaForaDoEscopo()
    {
        var act = () => TipoVeiculoExtensions.ParseTipoVeiculo("moto");

        act.Should().Throw<TipoVeiculoNaoPermitidoException>();
    }

    [Fact]
    public void AtualizarVeiculo_OfertaElegivelQuePerdeLocalizacaoSemConfirmacao_LancaEReverteEmMemoria()
    {
        var now = new DateTimeOffset(2026, 8, 16, 12, 30, 0, TimeSpan.Zero);
        var oferta = CriarOfertaElegivel(now);
        var autoriaOriginal = oferta.AtualizadaPor;
        var atualizadoEmOriginal = oferta.AtualizadoEm;
        var patch = LimparCidadePatch();

        var act = () => oferta.AtualizarVeiculo(
            patch,
            new Autoria("operator-43", "Bruno Lima", now.AddMinutes(1)),
            now.AddMinutes(1),
            confirmaSuspensao: false);

        var exception = act.Should().Throw<SuspensaoNaoConfirmadaException>().Which;

        exception.CriteriosAfetados.Should().Equal(CodigoCriterio.Localizacao);
        oferta.Situacao.Should().Be(SituacaoOferta.Elegivel);
        oferta.Veiculo.Localizacao.Cidade.Should().Be("Campinas");
        oferta.AtualizadaPor.Should().Be(autoriaOriginal);
        oferta.AtualizadoEm.Should().Be(atualizadoEmOriginal);
        oferta.MotivoSuspensao.Should().BeNull();
        oferta.SuspensaEm.Should().BeNull();
    }

    [Fact]
    public void AtualizarVeiculo_OfertaElegivelComConfirmacao_AplicaAlteracaoESuspende()
    {
        var now = new DateTimeOffset(2026, 8, 16, 12, 30, 0, TimeSpan.Zero);
        var oferta = CriarOfertaElegivel(now);
        var atualizadaEm = now.AddMinutes(1);
        var autoria = new Autoria("operator-43", "Bruno Lima", atualizadaEm);

        oferta.AtualizarVeiculo(LimparCidadePatch(), autoria, atualizadaEm, confirmaSuspensao: true);

        oferta.Situacao.Should().Be(SituacaoOferta.Suspensa);
        oferta.Veiculo.Localizacao.Cidade.Should().BeNull();
        oferta.SuspensaEm.Should().Be(atualizadaEm);
        oferta.MotivoSuspensao.Should().Contain("localizacao");
        oferta.AtualizadaPor.Should().Be(autoria);
    }

    [Fact]
    public void ExcluirOferta_EmPreparacao_PermiteExclusao()
    {
        var now = DateTimeOffset.UtcNow;
        var oferta = Oferta.Criar(
            new Veiculo(TipoVeiculo.CarroUsado, placa: "ABC1D23"),
            new Autoria("operator-42", "Ana Souza", now),
            now);

        var act = () => oferta.Excluir();

        act.Should().NotThrow();
    }

    [Fact]
    public void ExcluirOferta_ForaDePreparacao_LancaExcecaoDeDominio()
    {
        var oferta = CriarOfertaElegivel(DateTimeOffset.UtcNow);

        var act = () => oferta.Excluir();

        act.Should().Throw<OfertaNaoExcluivelException>();
    }

    [Fact]
    public void AtualizarVeiculo_OfertaForaDePreparacaoComPlaca_LancaPlacaImutavel()
    {
        var oferta = CriarOfertaElegivel(DateTimeOffset.UtcNow);
        var patch = new VeiculoPatch
        {
            PlacaInformada = true,
            Placa = "DEF4G56",
        };

        var act = () => oferta.AtualizarVeiculo(
            patch,
            new Autoria("operator-43", "Bruno Lima", DateTimeOffset.UtcNow),
            DateTimeOffset.UtcNow,
            confirmaSuspensao: true);

        act.Should().Throw<PlacaImutavelException>();
        oferta.Veiculo.Placa.Should().Be("ABC1D23");
        oferta.Situacao.Should().Be(SituacaoOferta.Elegivel);
    }

    private static VeiculoPatch LimparCidadePatch() => new()
    {
        LocalizacaoInformada = true,
        Localizacao = new LocalizacaoPatch
        {
            CidadeInformada = true,
            Cidade = null,
        },
    };

    private static Oferta CriarOfertaElegivel(DateTimeOffset now)
    {
        var autoria = new Autoria("operator-42", "Ana Souza", now);
        var oferta = Oferta.Criar(
            new Veiculo(
                TipoVeiculo.CarroSeminovo,
                placa: "ABC1D23",
                marca: "Honda",
                modelo: "Civic",
                versao: "EXL",
                anoFabricacao: 2021,
                anoModelo: 2022,
                quilometragem: 48300,
                cambio: "Automático",
                localizacao: new Localizacao("13010-111", "Campinas", "SP")),
            autoria,
            now);

        var fatos = CriarFatosCompletos(autoria);
        SetProperty(oferta, nameof(Oferta.Fatos), fatos);
        SetProperty(oferta, nameof(Oferta.PrecoOficial), new PrecoOficial(8_790_000, autoria));
        SetProperty(oferta.Disponibilidade, nameof(Disponibilidade.EstadoConhecido), true);
        SetProperty(oferta, nameof(Oferta.Situacao), SituacaoOferta.Elegivel);
        return oferta;
    }

    private static FatosConhecidos CriarFatosCompletos(Autoria autoria)
    {
        var blocoOrigem = new BlocoFato(BlocoFatoTipo.Origem, descricao: "Origem conhecida", atualizadoPor: autoria);
        var blocoCondicao = new BlocoFato(BlocoFatoTipo.Condicao, descricao: "Condição conhecida", atualizadoPor: autoria);
        var blocoHistorico = new BlocoFato(BlocoFatoTipo.Historico, descricao: "Histórico conhecido", atualizadoPor: autoria);
        var constructor = typeof(FatosConhecidos).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(BlocoFato), typeof(BlocoFato), typeof(BlocoFato)],
            modifiers: null);

        return (FatosConhecidos)constructor!.Invoke([blocoOrigem, blocoCondicao, blocoHistorico]);
    }

    private static void SetProperty<T>(T target, string propertyName, object? value)
    {
        typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(target, value);
    }
}
