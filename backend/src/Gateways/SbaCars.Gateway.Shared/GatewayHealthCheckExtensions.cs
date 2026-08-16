using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SbaCars.BuildingBlocks.Observability;

namespace SbaCars.Gateway.Shared;

/// <summary>
/// Wires the two gateways' health checks (§8): the self check every process gets
/// (<see cref="SbaCarsHealthChecksExtensions.AddSbaCarsHealthChecks"/>) plus
/// <see cref="DownstreamReadinessHealthCheck"/>, tagged <see cref="HealthCheckTags.Ready"/> so it
/// answers <c>/health/ready</c> alongside the self check. Called by both gateways, identically.
/// Returns the builder so <c>gateway-backoffice</c> can chain its own extra leg —
/// <c>AddSbaCarsJwksReadinessCheck</c> — on top, since it is the one gateway that also calls
/// <c>AddSbaCarsAuth</c> (§5.2); <c>gateway-public</c> never does (§5.5) and stops here.
/// </summary>
public static class GatewayHealthCheckExtensions
{
    public static IHealthChecksBuilder AddSbaCarsGatewayHealthChecks(this IServiceCollection services)
    {
        services.AddHttpClient(
            DownstreamReadinessHealthCheck.HttpClientName,
            client => client.Timeout = TimeSpan.FromSeconds(5));

        return services.AddSbaCarsHealthChecks()
            .AddCheck<DownstreamReadinessHealthCheck>("downstream-services", tags: [HealthCheckTags.Ready]);
    }
}
