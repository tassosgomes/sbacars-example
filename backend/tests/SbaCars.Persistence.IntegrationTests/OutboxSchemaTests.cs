using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Inventory.Infrastructure;
using SbaCars.TestKit;
using Xunit;

namespace SbaCars.Persistence.IntegrationTests;

/// <summary>
/// B2 schema placement (§6.2): the Rebus outbox table lives in the owning service schema and
/// remains unreachable across the PostgreSQL grant boundary.
/// </summary>
[Collection(SbaCarsPostgresCollection.Name)]
public sealed class OutboxSchemaTests
{
    private readonly SbaCarsPostgresFixture _fixture;

    public OutboxSchemaTests(SbaCarsPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task InventoryMigrator_CreatesOutboxTable_InInventorySchema()
    {
        var ownerConnectionString = _fixture.ConnectionStringFor("own_inventory", "own_inventory_dev_pw");
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(ownerConnectionString, InventoryDbContext.Schema);

        await using var context = new InventoryDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        await using var connection = new NpgsqlConnection(
            _fixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT 1
            FROM information_schema.tables
            WHERE table_schema = 'inventory'
              AND table_name = 'outbox';
            """;

        var exists = await command.ExecuteScalarAsync();
        exists.Should().NotBeNull("inventory.outbox must exist after migrations are applied");
    }

    [Fact]
    public async Task SvcCatalog_CannotReadInventoryOutbox()
    {
        var ownerConnectionString = _fixture.ConnectionStringFor("own_inventory", "own_inventory_dev_pw");
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(ownerConnectionString, InventoryDbContext.Schema);

        await using (var context = new InventoryDbContext(optionsBuilder.Options))
        {
            await context.Database.MigrateAsync();
        }

        await using var connection = new NpgsqlConnection(
            _fixture.ConnectionStringFor("svc_catalog", "svc_catalog_dev_pw"));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM inventory.outbox;";

        var reading = async () => await command.ExecuteScalarAsync();

        var assertion = await reading.Should().ThrowAsync<PostgresException>();
        assertion.Which.SqlState.Should().Be(
            PostgresErrorCodes.InsufficientPrivilege,
            "svc_catalog must not read inventory.outbox — the boundary is physical");
    }
}
