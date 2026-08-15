using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SbaCars.BuildingBlocks.Persistence;
using Xunit;

namespace SbaCars.Persistence.IntegrationTests;

/// <summary>
/// Proves the second half of A4's readiness criterion, and the reason the whole task exists
/// (§4.1 of the foundation plan): the boundary between services is enforced by PostgreSQL grants,
/// not by convention. Covers both directions — <c>svc_catalog</c> reading/writing its own schema
/// must work, and reading a schema it was never granted must fail with a permission error, not
/// silently return nothing.
/// </summary>
[Collection(SbaCarsPostgresCollection.Name)]
public sealed class SchemaIsolationTests
{
    private readonly SbaCarsPostgresFixture _fixture;

    public SchemaIsolationTests(SbaCarsPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SvcCatalog_CanReadAndWriteOwnSchema_AndEntityMaterializesWithThePersistedId()
    {
        await CreateProbeTableAsync("catalog", "own_catalog", "own_catalog_dev_pw");

        var appConnectionString = _fixture.ConnectionStringFor("svc_catalog", "svc_catalog_dev_pw");
        var optionsBuilder = new DbContextOptionsBuilder<ProbeDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(appConnectionString, "catalog");

        var probe = new ProbeEntity("acme");
        var originalId = probe.Id;
        originalId.Should().NotBe(Guid.Empty, "Entity's parameterless constructor path must still assign a version-7 id");

        await using (var writeContext = new ProbeDbContext(optionsBuilder.Options, "catalog"))
        {
            writeContext.Probes.Add(probe);
            await writeContext.SaveChangesAsync();
        }

        ProbeEntity? reloaded;
        await using (var readContext = new ProbeDbContext(optionsBuilder.Options, "catalog"))
        {
            reloaded = await readContext.Probes.SingleOrDefaultAsync(p => p.Name == "acme");
        }

        // This is the proof that EF Core materializes SbaCars.BuildingBlocks.Domain.Entity
        // correctly despite its protected `init` id and Guid-generating parameterless
        // constructor: the id read back from PostgreSQL must be the one SaveChanges persisted,
        // not a fresh Guid minted by the constructor EF calls to materialize the row.
        reloaded.Should().NotBeNull();
        reloaded!.Id.Should().Be(originalId);
        reloaded.Name.Should().Be("acme");
    }

    [Fact]
    public async Task SvcCatalog_CannotReadInventorySchema()
    {
        await CreateProbeTableAsync("inventory", "own_inventory", "own_inventory_dev_pw");

        var connectionString = _fixture.ConnectionStringFor("svc_catalog", "svc_catalog_dev_pw");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM inventory.probe;";

        var reading = async () => await command.ExecuteScalarAsync();

        var assertion = await reading.Should().ThrowAsync<PostgresException>();
        assertion.Which.SqlState.Should().Be(
            PostgresErrorCodes.InsufficientPrivilege,
            "svc_catalog was never granted USAGE on the inventory schema — the boundary is physical, not a convention");
    }

    private async Task CreateProbeTableAsync(string schema, string ownerUsername, string ownerPassword)
    {
        var ownerConnectionString = _fixture.ConnectionStringFor(ownerUsername, ownerPassword);
        var optionsBuilder = new DbContextOptionsBuilder<ProbeDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(ownerConnectionString, schema);

        await using var context = new ProbeDbContext(optionsBuilder.Options, schema);
        await context.Database.EnsureCreatedAsync();
    }
}
