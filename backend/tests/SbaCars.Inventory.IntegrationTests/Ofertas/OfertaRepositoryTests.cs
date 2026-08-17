using System.Reflection;

using Microsoft.EntityFrameworkCore;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Inventory.Application.Ofertas.ListarOfertas;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Infrastructure;
using SbaCars.Inventory.Infrastructure.Ofertas;
using SbaCars.TestKit;

using InventoryTestRepositories = SbaCars.Inventory.IntegrationTests.InventoryTestRepositories;

namespace SbaCars.Inventory.IntegrationTests.Ofertas;

[Collection(InventoryPostgresCollection.Name)]
public sealed class OfertaDetalheTests
{
    private readonly SbaCarsPostgresFixture _fixture;

    public OfertaDetalheTests(SbaCarsPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Repository_PersistsPartialOffer_AndListsWithFiltersAndPagination()
    {
        await EnsureSchemaAsync();
        await ClearOffersAsync();

        var now = DateTimeOffset.UtcNow;
        await using var context = CreateContext(
            _fixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"));

        context.Ofertas.AddRange(
            Oferta.Criar(
                new Veiculo(
                    TipoVeiculo.CarroSeminovo,
                    placa: "ABC1D23",
                    marca: "Honda",
                    modelo: "Civic",
                    localizacao: new Localizacao(null, "Campinas", "SP")),
                new Autoria("operator-1", "Ana", now),
                now),
            Oferta.Criar(
                new Veiculo(
                    TipoVeiculo.CarroUsado,
                    placa: "DEF4G56",
                    marca: "Toyota",
                    modelo: "Corolla",
                    localizacao: new Localizacao(null, "São Paulo", "SP")),
                new Autoria("operator-2", "Bruno", now.AddMinutes(1)),
                now.AddMinutes(1)));
        await context.SaveChangesAsync();

        var repository = InventoryTestRepositories.CreateOfertaRepository(context);
        var result = await repository.ListarAsync(
            new ListarOfertasQuery
            {
                Page = 1,
                PageSize = 1,
                Busca = "Honda",
                Situacao = ["em-preparacao"],
                Uf = "sp",
            },
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items.Single().Situacao.Should().Be("em-preparacao");
        result.Items.Single().Placa.Should().Be("ABC1D23");
        result.Items.Single().Localizacao.Uf.Should().Be("SP");
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(1);
    }

    [Fact]
    public async Task Repository_EnforcesUniqueActivePlate()
    {
        await EnsureSchemaAsync();
        await ClearOffersAsync();

        var now = DateTimeOffset.UtcNow;
        await using var context = CreateContext(
            _fixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"));
        var autoria = new Autoria("operator-1", "Ana", now);

        context.Ofertas.Add(Oferta.Criar(
            new Veiculo(TipoVeiculo.CarroUsado, placa: "ABC1D23"), autoria, now));
        await context.SaveChangesAsync();

        context.Ofertas.Add(Oferta.Criar(
            new Veiculo(TipoVeiculo.CarroUsado, placa: "ABC-1D23"), autoria, now));

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Repository_ProjectsOfferDetailWithChecklistAndEmptyPendencies()
    {
        await EnsureSchemaAsync();
        await ClearOffersAsync();

        var now = DateTimeOffset.UtcNow;
        var oferta = Oferta.Criar(
            new Veiculo(TipoVeiculo.CarroSeminovo, placa: "ABC1D23"),
            new Autoria("operator-1", "Ana", now),
            now);

        await using (var writeContext = CreateContext(
                         _fixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw")))
        {
            writeContext.Ofertas.Add(oferta);
            await writeContext.SaveChangesAsync();
        }

        await using var context = CreateContext(
            _fixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"));
        var repository = InventoryTestRepositories.CreateOfertaRepository(context);

        var response = await repository.ObterDetalheAsync(oferta.Id, CancellationToken.None);

        response.Should().NotBeNull();
        response!.OfertaId.Should().Be(oferta.Id);
        response.Elegibilidade.Total.Should().Be(6);
        response.Elegibilidade.Atendidos.Should().Be(1);
        response.Elegibilidade.PodeSolicitarElegibilidade.Should().BeFalse();
        response.Elegibilidade.Criterios
            .Should().OnlyContain(criterio => criterio.Atendido || !string.IsNullOrWhiteSpace(criterio.Pendencia));
        response.Disponibilidade.TransicoesPermitidas.Should().Equal("reservado", "vendido");
        response.Fatos.Origem.AtendeTransparencia.Should().BeFalse();
        response.Fatos.Condicao.AtendeTransparencia.Should().BeFalse();
        response.Fatos.Historico.AtendeTransparencia.Should().BeFalse();
        response.Pendencias.Should().BeEmpty();
    }

    [Fact]
    public async Task Repository_UnknownOfferReturnsNullForApplicationNotFoundMapping()
    {
        await EnsureSchemaAsync();
        await using var context = CreateContext(
            _fixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"));
        var repository = InventoryTestRepositories.CreateOfertaRepository(context);

        var response = await repository.ObterDetalheAsync(Guid.NewGuid(), CancellationToken.None);

        response.Should().BeNull();
    }

    [Fact]
    public async Task Repository_SuspensionProtocol_DoesNotPersistWithoutConfirmation_AndPersistsWithConfirmation()
    {
        await EnsureSchemaAsync();
        await ClearOffersAsync();

        var now = DateTimeOffset.UtcNow;
        var oferta = CreateEligibleOffer(now);
        await using (var setupContext = CreateContext(
                         _fixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw")))
        {
            setupContext.Ofertas.Add(oferta);
            await setupContext.SaveChangesAsync();
        }

        var patch = new VeiculoPatch
        {
            LocalizacaoInformada = true,
            Localizacao = new LocalizacaoPatch
            {
                CidadeInformada = true,
                Cidade = null,
            },
        };

        await using (var rollbackContext = CreateContext(
                         _fixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw")))
        {
            var tracked = await rollbackContext.Ofertas
                .AsTracking()
                .SingleAsync(item => item.Id == oferta.Id);
            var act = () => tracked.AtualizarVeiculo(
                patch,
                new Autoria("operator-43", "Bruno Lima", now.AddMinutes(1)),
                now.AddMinutes(1),
                confirmaSuspensao: false);

            act.Should().Throw<SuspensaoNaoConfirmadaException>();
            rollbackContext.ChangeTracker.HasChanges().Should().BeFalse();
        }

        await using (var afterRollbackContext = CreateContext(
                         _fixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw")))
        {
            var persisted = await afterRollbackContext.Ofertas
                .AsNoTracking()
                .SingleAsync(item => item.Id == oferta.Id);

            persisted.Situacao.Should().Be(SituacaoOferta.Elegivel);
            persisted.Veiculo.Localizacao.Cidade.Should().Be("Campinas");
            persisted.MotivoSuspensao.Should().BeNull();
            persisted.SuspensaEm.Should().BeNull();
        }

        await using (var confirmationContext = CreateContext(
                         _fixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw")))
        {
            var tracked = await confirmationContext.Ofertas
                .AsTracking()
                .SingleAsync(item => item.Id == oferta.Id);
            tracked.Situacao.Should().Be(SituacaoOferta.Elegivel);
            tracked.AtualizarVeiculo(
                patch,
                new Autoria("operator-43", "Bruno Lima", now.AddMinutes(2)),
                now.AddMinutes(2),
                confirmaSuspensao: true);
            tracked.Situacao.Should().Be(SituacaoOferta.Suspensa);
            confirmationContext.ChangeTracker.HasChanges().Should().BeTrue();

            await confirmationContext.SaveChangesAsync();
        }

        await using var afterConfirmationContext = CreateContext(
            _fixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"));
        var confirmed = await afterConfirmationContext.Ofertas
            .AsNoTracking()
            .SingleAsync(item => item.Id == oferta.Id);

        confirmed.Situacao.Should().Be(SituacaoOferta.Suspensa);
        confirmed.Veiculo.Localizacao.Cidade.Should().BeNull();
        confirmed.MotivoSuspensao.Should().Contain("localizacao");
        confirmed.SuspensaEm.Should().NotBeNull();
    }

    [Fact]
    public async Task Repository_SubstituiFatos_AlternaCm6AndPersistsLimitation()
    {
        await EnsureSchemaAsync();
        await ClearOffersAsync();

        var now = DateTimeOffset.UtcNow;
        var autoria = new Autoria("operator-1", "Ana", now);
        var oferta = Oferta.Criar(
            new Veiculo(TipoVeiculo.CarroSeminovo, placa: "ABC1D23"),
            autoria,
            now);
        var fatos = FatosConhecidos.Criar(
            new BlocoFato(BlocoFatoTipo.Origem, descricao: "Origem conhecida", atualizadoPor: autoria),
            new BlocoFato(BlocoFatoTipo.Condicao, fonte: "Laudo interno", atualizadoPor: autoria),
            new BlocoFato(
                BlocoFatoTipo.Historico,
                indisponivel: true,
                limitacaoDeclarada: "Histórico não localizado.",
                atualizadoPor: autoria));

        oferta.SubstituirFatos(fatos, autoria, now, confirmaSuspensao: false);
        oferta.Fatos.AtendeTransparencia.Should().BeTrue();
        oferta.AvaliarCriteriosMinimos().Should().NotContain(CodigoCriterio.TransparenciaFatos);

        await using (var writeContext = CreateContext(
                         _fixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw")))
        {
            writeContext.Ofertas.Add(oferta);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext(
            _fixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"));
        var response = await InventoryTestRepositories.CreateOfertaRepository(readContext)
            .ObterDetalheAsync(oferta.Id, CancellationToken.None);

        response.Should().NotBeNull();
        response!.Fatos.Origem.Descricao.Should().Be("Origem conhecida");
        response.Fatos.Condicao.Fonte.Should().Be("Laudo interno");
        response.Fatos.Historico.Indisponivel.Should().BeTrue();
        response.Fatos.Historico.Descricao.Should().BeNull();
        response.Fatos.Historico.LimitacaoDeclarada.Should().Be("Histórico não localizado.");
        response.Fatos.Historico.AtendeTransparencia.Should().BeTrue();
    }

    [Fact]
    public async Task Repository_SubstituirFatos_409CandidateDoesNotPersist_ThenConfirmationSuspends()
    {
        await EnsureSchemaAsync();
        await ClearOffersAsync();

        var now = DateTimeOffset.UtcNow;
        var oferta = CreateEligibleOffer(now);
        await using (var setupContext = CreateContext(
                         _fixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw")))
        {
            setupContext.Ofertas.Add(oferta);
            await setupContext.SaveChangesAsync();
        }

        await using (var rollbackContext = CreateContext(
                         _fixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw")))
        {
            var tracked = await rollbackContext.Ofertas
                .AsTracking()
                .SingleAsync(item => item.Id == oferta.Id);

            var act = () => tracked.SubstituirFatos(
                FatosConhecidos.Vazios(),
                new Autoria("operator-43", "Bruno Lima", now.AddMinutes(1)),
                now.AddMinutes(1),
                confirmaSuspensao: false);

            var exception = act.Should().Throw<SuspensaoNaoConfirmadaException>().Which;
            exception.CriteriosAfetados.Should().Equal(CodigoCriterio.TransparenciaFatos);
            rollbackContext.ChangeTracker.HasChanges().Should().BeFalse();
        }

        await using (var confirmationContext = CreateContext(
                         _fixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw")))
        {
            var tracked = await confirmationContext.Ofertas
                .AsTracking()
                .SingleAsync(item => item.Id == oferta.Id);

            tracked.SubstituirFatos(
                FatosConhecidos.Vazios(),
                new Autoria("operator-43", "Bruno Lima", now.AddMinutes(2)),
                now.AddMinutes(2),
                confirmaSuspensao: true);

            await confirmationContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext(
            _fixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"));
        var persisted = await readContext.Ofertas
            .AsNoTracking()
            .SingleAsync(item => item.Id == oferta.Id);

        persisted.Situacao.Should().Be(SituacaoOferta.Suspensa);
        persisted.Fatos.AtendeTransparencia.Should().BeFalse();
        persisted.Fatos.Origem.Descricao.Should().BeNull();
        persisted.MotivoSuspensao.Should().Contain("transparencia-fatos");
    }

    private static Oferta CreateEligibleOffer(DateTimeOffset now)
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

        SetProperty(oferta, nameof(Oferta.Fatos), CreateCompleteFacts(autoria));
        SetProperty(oferta, nameof(Oferta.PrecoOficial), new PrecoOficial(8_790_000, autoria));
        SetProperty(oferta.Disponibilidade, nameof(Disponibilidade.EstadoConhecido), true);
        SetProperty(oferta, nameof(Oferta.Situacao), SituacaoOferta.Elegivel);
        return oferta;
    }

    private static FatosConhecidos CreateCompleteFacts(Autoria autoria)
    {
        var constructor = typeof(FatosConhecidos).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(BlocoFato), typeof(BlocoFato), typeof(BlocoFato)],
            modifiers: null);
        var origem = new BlocoFato(BlocoFatoTipo.Origem, descricao: "Origem conhecida", atualizadoPor: autoria);
        var condicao = new BlocoFato(BlocoFatoTipo.Condicao, descricao: "Condição conhecida", atualizadoPor: autoria);
        var historico = new BlocoFato(BlocoFatoTipo.Historico, descricao: "Histórico conhecido", atualizadoPor: autoria);

        return (FatosConhecidos)constructor!.Invoke([origem, condicao, historico]);
    }

    private static void SetProperty<T>(T target, string propertyName, object? value)
    {
        typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(target, value);
    }

    private async Task EnsureSchemaAsync()
    {
        await using var context = CreateContext(
            _fixture.ConnectionStringFor("own_inventory", "own_inventory_dev_pw"));
        await context.Database.MigrateAsync();
    }

    private async Task ClearOffersAsync()
    {
        await using var context = CreateContext(
            _fixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"));
        await context.Ofertas.ExecuteDeleteAsync();
    }

    private static InventoryDbContext CreateContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(connectionString, InventoryDbContext.Schema);
        return new InventoryDbContext(optionsBuilder.Options);
    }
}
