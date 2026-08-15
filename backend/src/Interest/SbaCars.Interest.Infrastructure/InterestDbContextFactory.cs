using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SbaCars.BuildingBlocks.Persistence;

namespace SbaCars.Interest.Infrastructure;

/// <summary>
/// Design-time factory used only by <c>dotnet ef</c> when authoring migrations
/// (<c>dotnet tool run dotnet-ef migrations add ...</c>). Never used at runtime: the running
/// service and the Migrator each build their own <see cref="DbContextOptions{TContext}"/> from
/// configuration, with the role appropriate to each (app role for the service, owner role for
/// the Migrator).
/// </summary>
public sealed class InterestDbContextFactory : IDesignTimeDbContextFactory<InterestDbContext>
{
    public InterestDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SBACARS_INTEREST_MIGRATION_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=sbacars;Username=own_interest;Password=own_interest_dev_pw;";

        var optionsBuilder = new DbContextOptionsBuilder<InterestDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(connectionString, InterestDbContext.Schema);

        return new InterestDbContext(optionsBuilder.Options);
    }
}
