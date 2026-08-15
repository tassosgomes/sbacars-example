using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Purchase.Infrastructure;

// Applies pending migrations for the "purchase" schema, connected with the schema-owning role
// (own_purchase), never the application role. Runs as a standalone step ahead of the service's
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
        "The migrator needs a connection string for the schema-owning role (own_purchase).");
    return 1;
}

var optionsBuilder = new DbContextOptionsBuilder<PurchaseDbContext>();
optionsBuilder.UseSbaCarsNpgsql(connectionString, PurchaseDbContext.Schema);

try
{
    await using var context = new PurchaseDbContext(optionsBuilder.Options);

    Console.WriteLine($"[purchase-migrator] Applying pending migrations to schema '{PurchaseDbContext.Schema}'...");
    await context.Database.MigrateAsync();
    Console.WriteLine("[purchase-migrator] Migrations applied successfully.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[purchase-migrator] Migration failed: {ex}");
    return 1;
}
