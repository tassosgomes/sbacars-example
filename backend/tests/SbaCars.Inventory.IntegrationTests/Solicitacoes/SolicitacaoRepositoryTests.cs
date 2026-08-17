using Microsoft.EntityFrameworkCore;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Solicitacoes.ListarFilaValidacao;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Domain.Solicitacoes;
using SbaCars.Inventory.Infrastructure;
using SbaCars.Inventory.Infrastructure.Solicitacoes;
using SbaCars.TestKit;

namespace SbaCars.Inventory.IntegrationTests.Solicitacoes;

[Collection(InventoryPostgresCollection.Name)]
public sealed class SolicitacaoRepositoryTests
{
    private readonly SbaCarsPostgresFixture _fixture;

    public SolicitacaoRepositoryTests(SbaCarsPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Repository_ListsPendingRequestsInSlaOrderAndCountsOutsideSla()
    {
        await EnsureSchemaAsync();
        await ClearDataAsync();

        var sexta = new DateTimeOffset(2026, 8, 14, 9, 0, 0, TimeSpan.Zero);
        var segunda = new DateTimeOffset(2026, 8, 17, 10, 0, 0, TimeSpan.Zero);
        var oferta = CreateOffer();

        await using (var setup = CreateContext())
        {
            setup.Ofertas.Add(oferta);
            setup.Solicitacoes.AddRange(
                Solicitacao.Abrir(
                    oferta.Id,
                    TipoSolicitacao.Retirada,
                    null,
                    "Retirar por operação.",
                    new Autoria("operator-1", "Ana", sexta),
                    sexta),
                Solicitacao.Abrir(
                    oferta.Id,
                    TipoSolicitacao.Preco,
                    8_450_000,
                    "Ajuste de mercado.",
                    new Autoria("operator-2", "Bruno", sexta.AddMinutes(30)),
                    sexta.AddMinutes(30)));
            await setup.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var repository = new SolicitacaoRepository(context, new CalculadoraDiasUteis());
        var result = await repository.ListarAsync(
            new ListarFilaValidacaoQuery { Page = 1, PageSize = 20 },
            segunda,
            CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Select(item => item.Tipo).Should().Equal("retirada", "preco");
        result.Items.Should().OnlyContain(item => item.Status == "pendente");
        result.Items.Should().OnlyContain(item => item.ForaDoSla);

        var count = await repository.ContarPendentesAsync(segunda, CancellationToken.None);

        count.Total.Should().Be(2);
        count.ForaDoSla.Should().Be(2);
        count.PorTipo["retirada"].Should().Be(1);
        count.PorTipo["preco"].Should().Be(1);
        count.PorTipo["elegibilidade"].Should().Be(0);
        count.PorTipo["reversao-venda"].Should().Be(0);
    }

    [Fact]
    public async Task Repository_UniquePartialIndexRejectsDuplicatePendingType()
    {
        await EnsureSchemaAsync();
        await ClearDataAsync();

        var now = new DateTimeOffset(2026, 8, 17, 10, 0, 0, TimeSpan.Zero);
        var oferta = CreateOffer();
        var autoria = new Autoria("operator-1", "Ana", now);
        var first = Solicitacao.Abrir(
            oferta.Id,
            TipoSolicitacao.Retirada,
            null,
            "Primeira retirada.",
            autoria,
            now);
        var second = Solicitacao.Abrir(
            oferta.Id,
            TipoSolicitacao.Retirada,
            null,
            "Segunda retirada.",
            autoria,
            now.AddMinutes(1));

        await using (var setup = CreateContext())
        {
            setup.Ofertas.Add(oferta);
            setup.Solicitacoes.Add(first);
            await setup.SaveChangesAsync();
        }

        await using var duplicateContext = CreateContext();
        duplicateContext.Solicitacoes.Add(second);
        var act = () => duplicateContext.SaveChangesAsync();

        await act.Should().ThrowAsync<SolicitacaoPendenteDuplicadaException>();
    }

    private static Oferta CreateOffer()
    {
        var now = new DateTimeOffset(2026, 8, 14, 9, 0, 0, TimeSpan.Zero);
        return Oferta.Criar(
            new Veiculo(
                TipoVeiculo.CarroSeminovo,
                placa: $"ABC{Random.Shared.Next(1, 9)}D23",
                marca: "Honda",
                modelo: "Civic"),
            new Autoria("operator-1", "Ana", now),
            now);
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
