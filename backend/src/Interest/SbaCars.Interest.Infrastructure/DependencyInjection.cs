using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenTelemetry.Trace;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.BuildingBlocks.Persistence.Auditing;

namespace SbaCars.Interest.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Wires the <c>interest</c> schema's persistence: <see cref="InterestDbContext"/> against
    /// the application role (DML only), the matching <see cref="IUnitOfWork"/>, and the
    /// end-of-request sensitive-data audit flush (§5.7 — a no-op today, since this service has no
    /// <c>ISensitiveDataEntity</c> yet). Connection string is bound through
    /// <see cref="PersistenceOptions"/> with <c>ValidateOnStart</c> — missing configuration fails
    /// the process at boot (§4.4).
    /// </summary>
    public static IServiceCollection AddInterestInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSbaCarsPersistenceOptions(configuration);

        services.AddDbContext<InterestDbContext>((provider, options) =>
        {
            var persistence = provider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            options.UseSbaCarsNpgsql(persistence.ConnectionString, InterestDbContext.Schema);
        });

        services.AddScoped<IUnitOfWork, EfUnitOfWork<InterestDbContext>>();
        services.AddSbaCarsSensitiveDataAuditFlusher<InterestDbContext>();

        // A8 (§8): Npgsql's own tracing, composed onto whatever tracer provider
        // AddSbaCarsObservability registers in Program.cs — see the remark on this project's
        // .csproj for why it lives here rather than in BuildingBlocks.Observability.
        services.ConfigureOpenTelemetryTracerProvider((_, tracing) => tracing.AddNpgsql());

        return services;
    }
}
