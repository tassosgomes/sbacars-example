using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SbaCars.BuildingBlocks.Persistence;

namespace SbaCars.Catalog.Infrastructure;

/// <summary>
/// Design-time factory used only by <c>dotnet ef</c> when authoring migrations
/// (<c>dotnet tool run dotnet-ef migrations add ...</c>). Never used at runtime: the running
/// service and the Migrator each build their own <see cref="DbContextOptions{TContext}"/> from
/// configuration, with the role appropriate to each (app role for the service, owner role for
/// the Migrator).
/// </summary>
public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SBACARS_CATALOG_MIGRATION_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=sbacars;Username=own_catalog;Password=own_catalog_dev_pw;";

        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(connectionString, CatalogDbContext.Schema);

        return new CatalogDbContext(optionsBuilder.Options);
    }
}
