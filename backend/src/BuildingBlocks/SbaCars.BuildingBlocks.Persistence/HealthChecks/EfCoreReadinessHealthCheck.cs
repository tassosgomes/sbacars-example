using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SbaCars.BuildingBlocks.Persistence.HealthChecks;

/// <summary>
/// The Postgres leg of <c>/health/ready</c> (§8): can this service's own <typeparamref name="TContext"/>
/// open a connection right now. Generic over the <see cref="DbContext"/> rather than over a raw
/// connection string, on purpose — it reuses the exact same connection (role, pool, options) the
/// service's own requests use, so a green check means "requests can reach Postgres", not just
/// "some connection string happens to be valid".
/// </summary>
public sealed class EfCoreReadinessHealthCheck<TContext>(TContext dbContext) : IHealthCheck
    where TContext : DbContext
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Could not open a connection to PostgreSQL.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL connectivity check failed.", ex);
        }
    }
}
