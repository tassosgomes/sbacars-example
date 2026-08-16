using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;

namespace SbaCars.Gateway.Shared;

/// <summary>
/// YARP wiring shared by the two gateways (A7 of the architecture plan): loading the
/// <c>ReverseProxy</c> section, and a fail-fast check that runs before <c>app.Run()</c>. Only
/// <c>gateway-public</c> and <c>gateway-backoffice</c> call this — no service proxies to another
/// service (§2.4: no synchronous inter-service call in the base).
/// </summary>
/// <remarks>
/// <para>
/// The route table itself (which path prefix maps to which service, which methods are allowed,
/// which authorization/rate-limit policy applies) is deliberately <b>not</b> here: it is
/// per-gateway <c>appsettings.json</c>, because the two edges expose a different surface on
/// purpose (§5.5 vs §5.2) and hard-coding a shared shape would just reintroduce the coupling the
/// two-gateway split exists to avoid.
/// </para>
/// <para>
/// <b>Why this lives beside the gateways and not in <c>BuildingBlocks.Web</c>.</b> The
/// containment table of §3.3 spells out what <c>.Web</c> holds — JwtBearer and policies,
/// ProblemDetails, correlation-id, rate limit, CORS, OpenAPI — and a reverse proxy is not on that
/// list. Putting it there would make all four domain services carry
/// <c>Yarp.ReverseProxy</c> transitively, since every <c>Api</c> references <c>.Web</c>: a
/// service would ship the machinery for proxying to another service, which is precisely what
/// §2.4 forbids it from doing. Two consumers justify extracting the code; they do not justify
/// extracting it upward past its only two consumers.
/// </para>
/// </remarks>
public static class ReverseProxyExtensions
{
    private const string SectionName = "ReverseProxy";

    /// <summary>Registers YARP and loads its route/cluster configuration from <see cref="SectionName"/>.</summary>
    public static IServiceCollection AddSbaCarsReverseProxy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddReverseProxy().LoadFromConfig(configuration.GetSection(SectionName));
        return services;
    }

    /// <summary>
    /// Validates the loaded route table, then maps the proxy endpoints. Validation must run here —
    /// synchronously, before <c>app.Run()</c> — and not be skipped: YARP itself does not reject a
    /// route whose <c>ClusterId</c> names a cluster that does not exist, nor a cluster with zero
    /// destinations. Left unchecked, either mistake degrades into a silent 503 the first time a
    /// request hits that route, in an environment where nobody configured a destination, instead
    /// of failing at boot the way every other <c>ValidateOnStart</c> option in this codebase does.
    /// </summary>
    public static WebApplication UseSbaCarsReverseProxy(this WebApplication app)
    {
        ValidateRouteTable(app.Services);
        app.MapReverseProxy();
        return app;
    }

    private static void ValidateRouteTable(IServiceProvider services)
    {
        var config = services.GetRequiredService<IProxyConfigProvider>().GetConfig();

        var clusterIds = new HashSet<string>(
            config.Clusters.Select(cluster => cluster.ClusterId),
            StringComparer.Ordinal);

        foreach (var route in config.Routes)
        {
            if (route.ClusterId is not { Length: > 0 } clusterId || !clusterIds.Contains(clusterId))
            {
                throw new InvalidOperationException(
                    $"YARP route '{route.RouteId}' references cluster '{route.ClusterId}', " +
                    "which is not configured under ReverseProxy:Clusters.");
            }
        }

        var referencedClusterIds = new HashSet<string>(
            config.Routes.Select(route => route.ClusterId!),
            StringComparer.Ordinal);

        foreach (var cluster in config.Clusters)
        {
            if (!referencedClusterIds.Contains(cluster.ClusterId))
            {
                continue;
            }

            if (cluster.Destinations is null || cluster.Destinations.Count == 0)
            {
                throw new InvalidOperationException(
                    $"YARP cluster '{cluster.ClusterId}' is referenced by at least one route but has no " +
                    "Destinations configured. Every environment must set ReverseProxy:Clusters:" +
                    $"{cluster.ClusterId}:Destinations (see appsettings.Development.json for the local shape).");
            }
        }
    }
}
