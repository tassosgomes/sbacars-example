using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using SbaCars.BuildingBlocks.Persistence;
using Xunit;

namespace SbaCars.Persistence.IntegrationTests;

/// <summary>
/// Closes the §12 debt tracked since A6b: without <see cref="SchemaAwareModelCacheKeyFactory"/>,
/// the same <see cref="ProbeDbContext"/> CLR type built for two different schemas in one process
/// would compile the model once and reuse it for both — so the second schema's
/// <see cref="DbContext.Database"/> operations would silently target the first schema's tables
/// instead. This is exactly the scenario <c>SensitiveDataAuditFlushMiddlewareTests</c>' old comment
/// described working around by giving two *different test classes* the same schema; here, the same
/// context *type* targets two different schemas within a single test, which was not possible to
/// write correctly before the fix.
/// </summary>
/// <remarks>
/// Asserts on <c>Database.GenerateCreateScript()</c> — the DDL Npgsql's provider compiles straight
/// from the context's model — rather than on <c>EnsureCreatedAsync</c> against a live database.
/// The first attempt at this test used <c>EnsureCreatedAsync</c> and failed for the wrong reason:
/// EF Core's relational <c>EnsureCreated</c> treats "the target database already has ANY tables
/// anywhere" (not "the tables this model needs") as "nothing to do", so it silently no-ops on the
/// second schema regardless of which model got compiled — proving nothing either way. Reading the
/// generated DDL text is a direct, offline check of what the compiled model actually says, with no
/// dependency on what tables happen to exist already — and, since <c>GenerateCreateScript()</c>
/// never opens a connection, no need for a real Postgres container at all (unlike every other test
/// in this project).
/// </remarks>
public sealed class ModelCacheKeyFactoryTests
{
    [Fact]
    public void SameProbeContextType_BuiltForTwoDifferentSchemas_CompilesAModelPerSchema()
    {
        var catalogScript = GenerateCreateScript("catalog");
        var purchaseScript = GenerateCreateScript("purchase");

        catalogScript.Should().Contain(
            "CREATE TABLE catalog.probe",
            "the first ProbeDbContext instance, built for the 'catalog' schema, must compile a model targeting it");

        purchaseScript.Should().Contain(
            "CREATE TABLE purchase.probe",
            "a second ProbeDbContext instance of the very same CLR type, built for the 'purchase' schema, must " +
            "compile its own model instead of reusing the 'catalog' schema's cached one — before " +
            "SchemaAwareModelCacheKeyFactory, this script would still say 'CREATE TABLE catalog.probe'");

        purchaseScript.Should().NotContain(
            "CREATE TABLE catalog.probe",
            "the 'purchase' schema's script must not still carry the first schema's compiled model");
    }

    private static string GenerateCreateScript(string schema)
    {
        // Syntactically valid but never dialed: GenerateCreateScript() compiles DDL straight from
        // the model without opening a connection, so no real Postgres container is needed here.
        const string ConnectionString = "Host=localhost;Database=sbacars;Username=test;Password=test";

        var optionsBuilder = new DbContextOptionsBuilder<ProbeDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(ConnectionString, schema);

        using var context = new ProbeDbContext(optionsBuilder.Options, schema);
        return context.Database.GenerateCreateScript();
    }
}
