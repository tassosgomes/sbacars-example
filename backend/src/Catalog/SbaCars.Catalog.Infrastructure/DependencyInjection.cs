using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Persistence;

namespace SbaCars.Catalog.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Wires the <c>catalog</c> schema's persistence: <see cref="CatalogDbContext"/> against
    /// the application role (DML only) and the matching <see cref="IUnitOfWork"/>. Connection
    /// string is bound through <see cref="PersistenceOptions"/> with <c>ValidateOnStart</c> —
    /// missing configuration fails the process at boot (§4.4).
    /// </summary>
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSbaCarsPersistenceOptions(configuration);

        services.AddDbContext<CatalogDbContext>((provider, options) =>
        {
            var persistence = provider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            options.UseSbaCarsNpgsql(persistence.ConnectionString, CatalogDbContext.Schema);
        });

        services.AddScoped<IUnitOfWork, EfUnitOfWork<CatalogDbContext>>();

        return services;
    }
}
