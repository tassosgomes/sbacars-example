using Microsoft.EntityFrameworkCore;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Infrastructure;
using SbaCars.TestKit;

using InventoryPostgresCollection = SbaCars.Inventory.IntegrationTests.InventoryPostgresCollection;
using InventoryTestRepositories = SbaCars.Inventory.IntegrationTests.InventoryTestRepositories;

namespace SbaCars.Inventory.IntegrationTests.Evidencias;

[Collection(InventoryPostgresCollection.Name)]
public sealed class EvidenciaRepositoryTests
{
    private readonly SbaCarsPostgresFixture _fixture;

    public EvidenciaRepositoryTests(SbaCarsPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Adicionar_Evidencia_PersisteMetadadosNoPostgres()
    {
        await EnsureSchemaAsync();
        await ClearDataAsync();

        var now = new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);
        var oferta = Oferta.Criar(
            new Veiculo(TipoVeiculo.CarroUsado, placa: "ABC1D23"),
            new Autoria("operator-1", "Ana", now),
            now);

        await using (var context = CreateContext())
        {
            context.Ofertas.Add(oferta);
            await context.SaveChangesAsync();
        }

        var evidencia = Evidencia.Criar(
            oferta.Id,
            "laudo-cautelar.pdf",
            "application/pdf",
            4194304,
            new Autoria("operator-1", "Ana", now),
            now);

        await using (var context = CreateContext())
        {
            var repository = InventoryTestRepositories.CreateEvidenciaRepository(context);
            repository.Adicionar(evidencia);
            await context.SaveChangesAsync();
        }

        await using var read = CreateContext();
        var persisted = await read.Evidencias
            .AsNoTracking()
            .SingleAsync(item => item.Id == evidencia.Id);

        persisted.OfertaId.Should().Be(oferta.Id);
        persisted.NomeArquivo.Should().Be("laudo-cautelar.pdf");
        persisted.TipoConteudo.Should().Be("application/pdf");
        persisted.TamanhoBytes.Should().Be(4194304);
        persisted.ObjectKey.Should().Contain(oferta.Id.ToString("N"));
    }

    private static readonly DateTimeOffset Now =
        new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    private async Task EnsureSchemaAsync()
    {
        await using var context = CreateContext(
            _fixture.ConnectionStringFor("own_inventory", "own_inventory_dev_pw"));
        await context.Database.MigrateAsync();
    }

    private async Task ClearDataAsync()
    {
        await using var context = CreateContext();
        await context.Evidencias.ExecuteDeleteAsync();
        await context.Solicitacoes.ExecuteDeleteAsync();
        await context.Ofertas.ExecuteDeleteAsync();
    }

    private InventoryDbContext CreateContext() => CreateContext(
        _fixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"));

    private static InventoryDbContext CreateContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(connectionString, InventoryDbContext.Schema);
        return new InventoryDbContext(optionsBuilder.Options);
    }
}
