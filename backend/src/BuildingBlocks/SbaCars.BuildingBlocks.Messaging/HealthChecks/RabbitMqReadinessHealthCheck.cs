using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SbaCars.BuildingBlocks.Messaging.HealthChecks;

/// <summary>
/// The RabbitMQ leg of <c>/health/ready</c> (§8, filling the gap the A8 task explicitly left open —
/// "RabbitMQ, S3/MinIO and Redis do not exist yet"): can this service reach the broker right now.
/// Delegates entirely to <see cref="RabbitMqConnectionProbe"/>'s shared, long-lived connection — see
/// that class' remarks for why this check never opens a connection of its own.
/// </summary>
public sealed class RabbitMqReadinessHealthCheck(RabbitMqConnectionProbe probe) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await probe.IsOpenAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Could not open a connection to RabbitMQ.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ connectivity check failed.", ex);
        }
    }
}
