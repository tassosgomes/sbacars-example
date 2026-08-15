using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Inventory.Infrastructure;

// Applies pending migrations for the "inventory" schema, connected with the schema-owning role
// (own_inventory), never the application role. Runs as a standalone step ahead of the service's
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
        "The migrator needs a connection string for the schema-owning role (own_inventory).");
    return 1;
}

var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
optionsBuilder.UseSbaCarsNpgsql(connectionString, InventoryDbContext.Schema);

try
{
    await using var context = new InventoryDbContext(optionsBuilder.Options);

    Console.WriteLine($"[inventory-migrator] Applying pending migrations to schema '{InventoryDbContext.Schema}'...");
    await context.Database.MigrateAsync();
    Console.WriteLine("[inventory-migrator] Migrations applied successfully.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[inventory-migrator] Migration failed: {ex}");
    return 1;
}
