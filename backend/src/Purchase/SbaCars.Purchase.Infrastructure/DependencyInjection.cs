using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.BuildingBlocks.Persistence.Auditing;

namespace SbaCars.Purchase.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Wires the <c>purchase</c> schema's persistence: <see cref="PurchaseDbContext"/> against
    /// the application role (DML only), the matching <see cref="IUnitOfWork"/>, and the
    /// end-of-request sensitive-data audit flush (§5.7 — a no-op today, since this service has no
    /// <c>ISensitiveDataEntity</c> yet). Connection string is bound through
    /// <see cref="PersistenceOptions"/> with <c>ValidateOnStart</c> — missing configuration fails
    /// the process at boot (§4.4).
    /// </summary>
    public static IServiceCollection AddPurchaseInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSbaCarsPersistenceOptions(configuration);

        services.AddDbContext<PurchaseDbContext>((provider, options) =>
        {
            var persistence = provider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            options.UseSbaCarsNpgsql(persistence.ConnectionString, PurchaseDbContext.Schema);
        });

        services.AddScoped<IUnitOfWork, EfUnitOfWork<PurchaseDbContext>>();
        services.AddSbaCarsSensitiveDataAuditFlusher<PurchaseDbContext>();

        return services;
    }
}
