using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Persistence;

namespace SbaCars.Inventory.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Wires the <c>inventory</c> schema's persistence: <see cref="InventoryDbContext"/> against
    /// the application role (DML only) and the matching <see cref="IUnitOfWork"/>. Connection
    /// string is bound through <see cref="PersistenceOptions"/> with <c>ValidateOnStart</c> —
    /// missing configuration fails the process at boot (§4.4).
    /// </summary>
    public static IServiceCollection AddInventoryInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSbaCarsPersistenceOptions(configuration);

        services.AddDbContext<InventoryDbContext>((provider, options) =>
        {
            var persistence = provider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            options.UseSbaCarsNpgsql(persistence.ConnectionString, InventoryDbContext.Schema);
        });

        services.AddScoped<IUnitOfWork, EfUnitOfWork<InventoryDbContext>>();

        return services;
    }
}
