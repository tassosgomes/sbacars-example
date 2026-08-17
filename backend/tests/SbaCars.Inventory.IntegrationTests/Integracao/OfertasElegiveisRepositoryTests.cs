using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Inventory.Application.Integracao.ListarOfertasElegiveis;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Infrastructure;
using SbaCars.Inventory.Infrastructure.Ofertas;
using SbaCars.TestKit;

using InventoryTestRepositories = SbaCars.Inventory.IntegrationTests.InventoryTestRepositories;

namespace SbaCars.Inventory.IntegrationTests.Integracao;

[Collection(InventoryPostgresCollection.Name)]
public sealed class OfertasElegiveisRepositoryTests
{
    private readonly SbaCarsPostgresFixture _fixture;

    public OfertasElegiveisRepositoryTests(SbaCarsPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Repository_ReturnsOnlyEligibleOffersAndProjectsPublicFacts()
    {
        await EnsureSchemaAsync();
        await ClearDataAsync();

        var now = new DateTimeOffset(2026, 8, 10, 9, 0, 0, TimeSpan.Zero);
        var eligible = CreateEligibleOffer("ABC1D23", now, now.AddMinutes(5));
        var preparing = Oferta.Criar(
            new Veiculo(TipoVeiculo.CarroUsado, placa: "DEF4G56"),
            new Autoria("operator-preparing", "Operador", now),
            now);
        var suspended = CreateEligibleOffer("GHI7J89", now.AddMinutes(10), now.AddMinutes(15));
        suspended.SubstituirFatos(
            FatosConhecidos.Vazios(),
            new Autoria("operator-suspension", "Operador", now.AddMinutes(16)),
            now.AddMinutes(16),
            confirmaSuspensao: true);
        var withdrawn = CreateEligibleOffer("KLM1N23", now.AddMinutes(20), now.AddMinutes(25));
        withdrawn.Retirar(
            new Autoria("operator-withdrawal", "Operador", now.AddMinutes(26)),
            now.AddMinutes(26));

        await using (var setup = CreateContext())
        {
            setup.Ofertas.AddRange(eligible, preparing, suspended, withdrawn);
            await setup.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var result = await InventoryTestRepositories.CreateOfertaRepository(context).ListarAsync(
            new ListarOfertasElegiveisQuery(),
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        var item = result.Items.Single();
        item.OfertaId.Should().Be(eligible.Id);
        item.Veiculo.Placa.Should().Be("ABC1D23");
        item.Veiculo.TipoVeiculo.Should().Be("carro-seminovo");
        item.Veiculo.Localizacao.Cidade.Should().Be("Campinas");
        item.PrecoOficial.ValorCentavos.Should().Be(8_790_000);
        item.PrecoOficial.Moeda.Should().Be("BRL");
        item.Disponibilidade.Should().Be("disponivel");
        item.Fatos.Historico.Indisponivel.Should().BeTrue();
        item.Fatos.Historico.LimitacaoDeclarada.Should().Be("Histórico de sinistros não obtido.");
        item.Fatos.Historico.AtendeTransparencia.Should().BeTrue();
        item.AtualizadoEm.Should().Be(eligible.AtualizadoEm);

        var payload = JsonSerializer.SerializeToElement(
            item,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        payload.TryGetProperty("checklist", out _).Should().BeFalse();
        payload.TryGetProperty("pendencias", out _).Should().BeFalse();
        payload.TryGetProperty("solicitacoes", out _).Should().BeFalse();
        payload.TryGetProperty("dadosValidacao", out _).Should().BeFalse();
        payload.GetProperty("fatos").GetProperty("origem").TryGetProperty("atualizadoPor", out _)
            .Should().BeFalse();
        payload.GetProperty("precoOficial").TryGetProperty("definidoPor", out _)
            .Should().BeFalse();
    }

    [Fact]
    public async Task Repository_FiltersAfterUtcInstantAndPaginatesWithStableOrdering()
    {
        await EnsureSchemaAsync();
        await ClearDataAsync();

        var baseTime = new DateTimeOffset(2026, 8, 11, 9, 0, 0, TimeSpan.Zero);
        var first = CreateEligibleOffer("ABC1D23", baseTime, baseTime.AddMinutes(5));
        var second = CreateEligibleOffer("DEF4G56", baseTime.AddMinutes(10), baseTime.AddMinutes(15));
        var third = CreateEligibleOffer("GHI7J89", baseTime.AddMinutes(20), baseTime.AddMinutes(25));

        await using (var setup = CreateContext())
        {
            setup.Ofertas.AddRange(first, second, third);
            await setup.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var repository = InventoryTestRepositories.CreateOfertaRepository(context);
        var firstPage = await repository.ListarAsync(
            new ListarOfertasElegiveisQuery { Page = 1, PageSize = 2 },
            CancellationToken.None);
        var secondPage = await repository.ListarAsync(
            new ListarOfertasElegiveisQuery { Page = 2, PageSize = 2 },
            CancellationToken.None);
        var incremental = await repository.ListarAsync(
            new ListarOfertasElegiveisQuery { AtualizadoApos = second.AtualizadoEm },
            CancellationToken.None);

        firstPage.TotalCount.Should().Be(3);
        firstPage.Items.Select(item => item.OfertaId).Should().Equal(third.Id, second.Id);
        firstPage.HasNextPage.Should().BeTrue();
        firstPage.HasPreviousPage.Should().BeFalse();
        secondPage.Items.Select(item => item.OfertaId).Should().Equal(first.Id);
        secondPage.HasNextPage.Should().BeFalse();
        secondPage.HasPreviousPage.Should().BeTrue();
        incremental.Items.Select(item => item.OfertaId).Should().Equal(third.Id);
        incremental.TotalCount.Should().Be(1);
    }

    private static Oferta CreateEligibleOffer(
        string plate,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        var autoria = new Autoria($"operator-{plate}", "Ana Souza", createdAt);
        var oferta = Oferta.Criar(
            new Veiculo(
                TipoVeiculo.CarroSeminovo,
                placa: plate,
                chassi: "93HFC2650MZ204817",
                marca: "Honda",
                modelo: "Civic",
                versao: "EXL 2.0",
                anoFabricacao: 2021,
                anoModelo: 2022,
                quilometragem: 48_300,
                cor: "Prata",
                combustivel: "Flex",
                cambio: "Automático",
                localizacao: new Localizacao("13010-111", "Campinas", "SP")),
            autoria,
            createdAt);

        oferta.SubstituirFatos(
            FatosConhecidos.Criar(
                new BlocoFato(
                    BlocoFatoTipo.Origem,
                    descricao: "Frota corporativa.",
                    fonte: "Contrato de cessão.",
                    atualizadoPor: autoria),
                new BlocoFato(
                    BlocoFatoTipo.Condicao,
                    descricao: "Revisões conhecidas.",
                    fonte: "Histórico de manutenção.",
                    atualizadoPor: autoria),
                new BlocoFato(
                    BlocoFatoTipo.Historico,
                    indisponivel: true,
                    limitacaoDeclarada: "Histórico de sinistros não obtido.",
                    atualizadoPor: autoria)),
            autoria,
            createdAt.AddMinutes(1),
            confirmaSuspensao: false);
        oferta.DefinirPrecoInicial(8_790_000, autoria.UsuarioId, autoria.Nome, createdAt.AddMinutes(2));
        oferta.AlterarDisponibilidade(
            EstadoDisponibilidade.Reservado,
            null,
            autoria,
            createdAt.AddMinutes(3));
        oferta.AlterarDisponibilidade(
            EstadoDisponibilidade.Disponivel,
            null,
            autoria,
            createdAt.AddMinutes(4));
        oferta.TornarElegivel(autoria, updatedAt);
        return oferta;
    }

    private async Task EnsureSchemaAsync()
    {
        await using var context = CreateContext("own_inventory", "own_inventory_dev_pw");
        await context.Database.MigrateAsync();
    }

    private async Task ClearDataAsync()
    {
        await using var context = CreateContext();
        await context.Solicitacoes.ExecuteDeleteAsync();
        await context.Ofertas.ExecuteDeleteAsync();
    }

    private InventoryDbContext CreateContext(
        string user = "svc_inventory",
        string password = "svc_inventory_dev_pw")
    {
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(
            _fixture.ConnectionStringFor(user, password),
            InventoryDbContext.Schema);
        return new InventoryDbContext(optionsBuilder.Options);
    }
}
