using Microsoft.Extensions.Diagnostics.HealthChecks;
using Yarp.ReverseProxy.Configuration;

namespace SbaCars.Gateway.Shared;

/// <summary>
/// The "gateways aggregate" leg of <c>/health/ready</c> (§8): queries <c>/health/ready</c> on one
/// destination per cluster this gateway routes to, straight from the same YARP cluster map
/// <see cref="ReverseProxyExtensions"/> already validates at boot — never a second, hand-maintained
/// list of services (§8 explicitly calls this out: "use o mapa de clusters da A7, não uma lista
/// duplicada à mão"). A gateway is only as ready to serve traffic as the services it fronts.
/// </summary>
public sealed class DownstreamReadinessHealthCheck(
    IProxyConfigProvider proxyConfigProvider,
    IHttpClientFactory httpClientFactory) : IHealthCheck
{
    /// <summary>Name of the <c>HttpClient</c> <see cref="GatewayHealthCheckExtensions"/> registers for this check.</summary>
    public const string HttpClientName = "sbacars-gateway-downstream-health";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var clusters = proxyConfigProvider.GetConfig().Clusters;
        if (clusters.Count == 0)
        {
            return HealthCheckResult.Healthy("No clusters configured.");
        }

        using var client = httpClientFactory.CreateClient(HttpClientName);

        var results = new Dictionary<string, object>();
        var allHealthy = true;

        foreach (var cluster in clusters)
        {
            var destination = cluster.Destinations?.Values.FirstOrDefault();
            if (destination is null)
            {
                // ReverseProxyExtensions.ValidateRouteTable already fails the boot for a
                // referenced cluster with no destination — an unreferenced one (none today, but
                // not impossible) is simply skipped here rather than reported unhealthy.
                continue;
            }

            var (healthy, detail) = await CheckDestinationAsync(client, destination.Address, cancellationToken);
            results[cluster.ClusterId] = detail;
            allHealthy &= healthy;
        }

        return allHealthy
            ? HealthCheckResult.Healthy("All downstream services are ready.", results)
            : HealthCheckResult.Unhealthy("At least one downstream service is not ready.", data: results);
    }

    private static async Task<(bool Healthy, string Detail)> CheckDestinationAsync(
        HttpClient client,
        string address,
        CancellationToken cancellationToken)
    {
        try
        {
            // The destination's readiness is queried directly, not through this gateway's own
            // proxied routes: /health/ready is not behind the /api prefix YARP rewrites (§8), and
            // aggregation must reach it regardless of which methods a route happens to allow.
            using var response = await client.GetAsync(new Uri(new Uri(address), "/health/ready"), cancellationToken);
            return response.IsSuccessStatusCode
                ? (true, $"{(int)response.StatusCode} {response.StatusCode}")
                : (false, $"{(int)response.StatusCode} {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return (false, $"unreachable: {ex.Message}");
        }
    }
}
