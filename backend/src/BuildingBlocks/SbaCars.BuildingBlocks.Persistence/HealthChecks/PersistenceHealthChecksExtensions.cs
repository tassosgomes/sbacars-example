using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SbaCars.BuildingBlocks.Persistence.HealthChecks;

/// <summary>
/// Registers <see cref="EfCoreReadinessHealthCheck{TContext}"/> against a service's own
/// <typeparamref name="TContext"/> — called once, from each of the four services' <c>Program.cs</c>
/// (§8), never from a gateway (gateways have no database).
/// </summary>
public static class PersistenceHealthChecksExtensions
{
    /// <summary>
    /// Adds the Postgres readiness check named <c>"postgres"</c>, tagged with whatever the caller
    /// passes — in practice always <c>SbaCars.BuildingBlocks.Observability.HealthCheckTags.Ready</c>,
    /// kept as a plain parameter here (rather than a direct reference to that constant) so this
    /// project does not need to depend on <c>BuildingBlocks.Observability</c> just to read one tag
    /// name.
    /// </summary>
    public static IHealthChecksBuilder AddSbaCarsPostgresReadinessCheck<TContext>(
        this IHealthChecksBuilder builder,
        params string[] tags)
        where TContext : DbContext =>
        builder.AddCheck<EfCoreReadinessHealthCheck<TContext>>("postgres", tags: tags);
}
