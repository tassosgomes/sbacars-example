using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Catalog.Infrastructure;

// Applies pending migrations for the "catalog" schema, connected with the schema-owning role
// (own_catalog), never the application role. Runs as a standalone step ahead of the service's
// rollout (§4.3) — the API never migrates itself outside Development.

var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetSection(PersistenceOptions.SectionName)["ConnectionString"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine(
        $"Missing configuration '{PersistenceOptions.SectionName}:ConnectionString'. " +
        "The migrator needs a connection string for the schema-owning role (own_catalog).");
    return 1;
}

var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
optionsBuilder.UseSbaCarsNpgsql(connectionString, CatalogDbContext.Schema);

try
{
    await using var context = new CatalogDbContext(optionsBuilder.Options);

    Console.WriteLine($"[catalog-migrator] Applying pending migrations to schema '{CatalogDbContext.Schema}'...");
    await context.Database.MigrateAsync();
    Console.WriteLine("[catalog-migrator] Migrations applied successfully.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[catalog-migrator] Migration failed: {ex}");
    return 1;
}
