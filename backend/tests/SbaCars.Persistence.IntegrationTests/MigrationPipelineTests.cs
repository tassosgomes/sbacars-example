using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Inventory.Infrastructure;
using SbaCars.TestKit;
using Xunit;

namespace SbaCars.Persistence.IntegrationTests;

/// <summary>
/// Proves the first half of A4's readiness criterion: the migration pipeline applies cleanly and
/// the history table lands inside the service's own schema — never <c>public</c>. Uses
/// <see cref="InventoryDbContext"/> connected as <c>own_inventory</c> (the schema-owning role),
/// exactly like the real <c>SbaCars.Inventory.Migrator</c> would.
/// </summary>
[Collection(SbaCarsPostgresCollection.Name)]
public sealed class MigrationPipelineTests
{
    private readonly SbaCarsPostgresFixture _fixture;

    public MigrationPipelineTests(SbaCarsPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Migrator_AppliesInCleanDatabase_AndHistoryLandsInTheServiceSchema()
    {
        var connectionString = _fixture.ConnectionStringFor("own_inventory", "own_inventory_dev_pw");
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(connectionString, InventoryDbContext.Schema);

        await using (var context = new InventoryDbContext(optionsBuilder.Options))
        {
            await context.Database.MigrateAsync();
        }

        await using var verifyConnection = new NpgsqlConnection(connectionString);
        await verifyConnection.OpenAsync();

        await using var command = verifyConnection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(*) FROM information_schema.tables
            WHERE table_schema = 'inventory' AND table_name = @tableName;
            """;
        command.Parameters.AddWithValue("tableName", SbaCarsDbContext.MigrationsHistoryTableName);

        var count = (long)(await command.ExecuteScalarAsync())!;

        count.Should().Be(1, "the migration history table must live inside the service's own schema, not public");
    }
}
