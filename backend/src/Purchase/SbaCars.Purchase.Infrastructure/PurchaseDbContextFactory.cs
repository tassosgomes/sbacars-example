using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SbaCars.BuildingBlocks.Persistence;

namespace SbaCars.Purchase.Infrastructure;

/// <summary>
/// Design-time factory used only by <c>dotnet ef</c> when authoring migrations
/// (<c>dotnet tool run dotnet-ef migrations add ...</c>). Never used at runtime: the running
/// service and the Migrator each build their own <see cref="DbContextOptions{TContext}"/> from
/// configuration, with the role appropriate to each (app role for the service, owner role for
/// the Migrator).
/// </summary>
public sealed class PurchaseDbContextFactory : IDesignTimeDbContextFactory<PurchaseDbContext>
{
    public PurchaseDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SBACARS_PURCHASE_MIGRATION_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=sbacars;Username=own_purchase;Password=own_purchase_dev_pw;";

        var optionsBuilder = new DbContextOptionsBuilder<PurchaseDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(connectionString, PurchaseDbContext.Schema);

        return new PurchaseDbContext(optionsBuilder.Options);
    }
}
