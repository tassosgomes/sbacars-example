extern alias GatewayBackoffice;
extern alias GatewayPublic;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using SbaCars.BuildingBlocks.Web.RateLimiting;

namespace SbaCars.Gateway.Tests;

/// <summary>
/// Reads each gateway's published <c>appsettings.json</c> straight off disk — no host, no
/// TestServer — and asserts the structural guarantees A7 exists to make impossible to violate by
/// accident (§2.3 of the architecture plan): the anonymous edge cannot proxy to inventory or
/// purchase, every anonymous route restricts its methods, every route points at a cluster that
/// actually exists, and every backoffice route requires authentication.
/// </summary>
public sealed class RouteTableTests
{
    [Fact]
    public void PublicGateway_HasNoRouteForInventoryOrPurchase()
    {
        var reverseProxy = LoadReverseProxyConfig(typeof(GatewayPublic::Program));
        var clusterIds = RouteClusterIds(reverseProxy);

        clusterIds.Should().NotContain("inventory");
        clusterIds.Should().NotContain("purchase");
    }

    [Fact]
    public void PublicGateway_OnlyRoutesToTheCatalogAndInterestClusters()
    {
        var reverseProxy = LoadReverseProxyConfig(typeof(GatewayPublic::Program));
        var clusterIds = RouteClusterIds(reverseProxy);

        clusterIds.Should().BeEquivalentTo(["catalog", "interest"]);
    }

    [Fact]
    public void PublicGateway_EveryRouteRestrictsItsMethods()
    {
        var reverseProxy = LoadReverseProxyConfig(typeof(GatewayPublic::Program));
        var routes = reverseProxy.GetSection("Routes").GetChildren().ToArray();

        routes.Should().NotBeEmpty();

        foreach (var route in routes)
        {
            var methods = route.GetSection("Match:Methods").GetChildren()
                .Select(child => child.Value)
                .ToArray();

            methods.Should().NotBeEmpty(
                $"anonymous route '{route.Key}' must restrict Match.Methods explicitly");
        }
    }

    [Fact]
    public void PublicGateway_InterestWriteRouteUsesTheAnonymousStrictRateLimitPolicy()
    {
        var reverseProxy = LoadReverseProxyConfig(typeof(GatewayPublic::Program));
        var interestRoutes = reverseProxy.GetSection("Routes").GetChildren()
            .Where(route => route["ClusterId"] == "interest")
            .ToArray();

        interestRoutes.Should().ContainSingle()
            .Which["RateLimiterPolicy"].Should().Be(RateLimitingPolicies.AnonymousStrict);
    }

    [Fact]
    public void BackofficeGateway_HasARouteForEachOfTheFourServices()
    {
        var reverseProxy = LoadReverseProxyConfig(typeof(GatewayBackoffice::Program));
        var clusterIds = RouteClusterIds(reverseProxy);

        clusterIds.Should().BeEquivalentTo(["inventory", "catalog", "interest", "purchase"]);
    }

    [Fact]
    public void BackofficeGateway_EveryRouteRequiresAnAuthenticatedUser()
    {
        var reverseProxy = LoadReverseProxyConfig(typeof(GatewayBackoffice::Program));
        var routes = reverseProxy.GetSection("Routes").GetChildren().ToArray();

        routes.Should().NotBeEmpty();
        routes.Should().OnlyContain(route => route["AuthorizationPolicy"] == "Default");
    }

    [Theory]
    [MemberData(nameof(BothGateways))]
    public void EveryRoute_PointsToAClusterThatExists(Type gatewayEntryPoint)
    {
        var reverseProxy = LoadReverseProxyConfig(gatewayEntryPoint);
        var clusterIds = new HashSet<string>(
            reverseProxy.GetSection("Clusters").GetChildren().Select(cluster => cluster.Key),
            StringComparer.Ordinal);
        var routeClusterIds = RouteClusterIds(reverseProxy);

        routeClusterIds.Should().OnlyContain(clusterId => clusterIds.Contains(clusterId));
    }

    [Theory]
    [MemberData(nameof(BothGateways))]
    public void EveryCluster_SetsAnExplicitActivityTimeout(Type gatewayEntryPoint)
    {
        var reverseProxy = LoadReverseProxyConfig(gatewayEntryPoint);
        var clusters = reverseProxy.GetSection("Clusters").GetChildren().ToArray();

        clusters.Should().NotBeEmpty();

        foreach (var cluster in clusters)
        {
            cluster["HttpRequest:ActivityTimeout"].Should().NotBeNullOrEmpty(
                $"cluster '{cluster.Key}' must set HttpRequest.ActivityTimeout explicitly (§8)");
        }
    }

    public static TheoryData<Type> BothGateways() =>
    [
        typeof(GatewayPublic::Program),
        typeof(GatewayBackoffice::Program),
    ];

    private static string[] RouteClusterIds(IConfigurationSection reverseProxy) =>
        reverseProxy.GetSection("Routes").GetChildren()
            .Select(route => route["ClusterId"])
            .Where(clusterId => clusterId is not null)
            .Select(clusterId => clusterId!)
            .ToArray();

    /// <summary>
    /// Loads only the base <c>appsettings.json</c> — the environment-independent route/cluster
    /// structure this test suite cares about — from the gateway's own content root.
    /// <c>appsettings.Development.json</c> (Destinations) is deliberately not merged in here:
    /// that split is exactly what this test would otherwise blur.
    /// </summary>
    /// <remarks>
    /// The content root comes from <c>MvcTestingAppManifest.json</c> — the manifest
    /// <c>Microsoft.AspNetCore.Mvc.Testing</c>'s build target generates next to this test
    /// assembly, mapping each referenced web-app project's assembly full name to its own source
    /// directory, and the same file <see cref="WebApplicationFactory{TEntryPoint}"/> itself reads
    /// to resolve a content root — rather than <c>gatewayEntryPoint.Assembly.Location</c>. This
    /// test project references both gateways, so their build outputs land in one shared
    /// directory; each Web SDK project ships a file literally named <c>appsettings.json</c>, and
    /// the last one copied there wins, silently masking the other. The manifest instead points at
    /// each gateway's own source directory.
    /// </remarks>
    private static IConfigurationSection LoadReverseProxyConfig(Type gatewayEntryPoint)
    {
        var manifestPath = Path.Combine(AppContext.BaseDirectory, "MvcTestingAppManifest.json");
        var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(manifestPath))
            ?? throw new InvalidOperationException($"Could not parse '{manifestPath}'.");

        var fullName = gatewayEntryPoint.Assembly.GetName().FullName;
        if (!manifest.TryGetValue(fullName, out var contentRoot))
        {
            throw new InvalidOperationException(
                $"No content root found for assembly '{fullName}' in '{manifestPath}'.");
        }

        var appsettingsPath = Path.Combine(contentRoot, "appsettings.json");

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(appsettingsPath, optional: false)
            .Build();

        return configuration.GetSection("ReverseProxy");
    }
}
