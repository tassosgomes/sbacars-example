using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Inventory.Infrastructure;
using SbaCars.TestKit;
using Xunit;

namespace SbaCars.Persistence.IntegrationTests;

/// <summary>
/// B6 schema placement (§2.5): saga and timeout tables live in the owning service schema and remain
/// unreachable across the PostgreSQL grant boundary.
/// </summary>
[Collection(SbaCarsPostgresCollection.Name)]
public sealed class SagaSchemaTests
{
    private readonly SbaCarsPostgresFixture _fixture;

    public SagaSchemaTests(SbaCarsPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task InventoryMigrator_CreatesSagaAndTimeoutTables_InInventorySchema()
    {
        var ownerConnectionString = _fixture.ConnectionStringFor("own_inventory", "own_inventory_dev_pw");
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(ownerConnectionString, InventoryDbContext.Schema);

        await using var context = new InventoryDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        await using var connection = new NpgsqlConnection(
            _fixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"));
        await connection.OpenAsync();

        foreach (var tableName in new[] { "sagas", "saga_index", "timeouts" })
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = 'inventory'
                  AND table_name = @table_name;
                """;
            command.Parameters.AddWithValue("table_name", tableName);

            var exists = await command.ExecuteScalarAsync();
            exists.Should().NotBeNull($"inventory.{tableName} must exist after migrations are applied");
        }
    }

    [Fact]
    public async Task SvcCatalog_CannotReadInventorySagas()
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

        foreach (var tableName in new[] { "sagas", "timeouts" })
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM inventory.{tableName};";

            var reading = async () => await command.ExecuteScalarAsync();

            var assertion = await reading.Should().ThrowAsync<PostgresException>();
            assertion.Which.SqlState.Should().Be(
                PostgresErrorCodes.InsufficientPrivilege,
                $"svc_catalog must not read inventory.{tableName} — the boundary is physical");
        }
    }
}
