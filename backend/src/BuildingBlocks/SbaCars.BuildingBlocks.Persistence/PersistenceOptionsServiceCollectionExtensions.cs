using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SbaCars.BuildingBlocks.Persistence;

public static class PersistenceOptionsServiceCollectionExtensions
{
    /// <summary>
    /// Binds <see cref="PersistenceOptions"/> from the <c>Persistence</c> configuration section
    /// with <c>ValidateOnStart</c> (§4.4): a missing or empty connection string fails the process
    /// at boot instead of the first request. Shared by every service and by each Migrator so the
    /// validation behavior is identical everywhere it applies.
    /// </summary>
    public static IServiceCollection AddSbaCarsPersistenceOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<PersistenceOptions>()
            .Bind(configuration.GetSection(PersistenceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
