extern alias GatewayPublic;

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SbaCars.Gateway.Tests;

/// <summary>
/// Drives gateway-public end to end through <see cref="WebApplicationFactory{TEntryPoint}"/>: real
/// pipeline, real YARP route table from <c>appsettings.json</c>/<c>appsettings.Development.json</c>,
/// only the cluster destination swapped for an in-process <see cref="DestinationStub"/> so no real
/// service needs to be running. No <c>[Authorize]</c> concerns here — gateway-public never wires
/// authentication (§5.5); <see cref="BackofficeGatewayBehaviorTests"/> covers the edge that does.
/// </summary>
public sealed class PublicGatewayBehaviorTests
{
    [Fact]
    public async Task CatalogPing_IsProxiedToTheDestination_WithThePathRewrittenToTheInternalShape()
    {
        await using var stub = await DestinationStub.StartAsync();
        await using var factory = CreateFactory(destinationOverrides: new Dictionary<string, string?>
        {
            ["ReverseProxy:Clusters:catalog:Destinations:destination1:Address"] = stub.Address,
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/catalog/_probe/ping");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stub.ReceivedAnyRequest.Should().BeTrue();
        // /api/catalog/{**rest} → PathRemovePrefix "/api/catalog" + PathPrefix "/api": the
        // gateway owns the public path map, catalog-service keeps a clean, edge-agnostic path.
        stub.LastRequestPath.Should().Be("/api/_probe/ping");
    }

    [Fact]
    public async Task InterestPing_IsProxiedToTheDestination_OverThePostOnlyAnonymousWriteRoute()
    {
        await using var stub = await DestinationStub.StartAsync();
        await using var factory = CreateFactory(destinationOverrides: new Dictionary<string, string?>
        {
            ["ReverseProxy:Clusters:interest:Destinations:destination1:Address"] = stub.Address,
        });
        using var client = factory.CreateClient();

        // POST, not GET: interest-write allows POST/OPTIONS only, because the anonymous surface
        // this route exists for is a write (§5.5). The probe endpoint is a POST to match.
        var response = await client.PostAsync("/api/interest/_probe/ping", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stub.LastRequestPath.Should().Be("/api/_probe/ping");
    }

    [Fact]
    public async Task InterestPath_WithGet_NeverReachesADestination_BecauseTheRouteIsWriteOnly()
    {
        await using var stub = await DestinationStub.StartAsync();
        await using var factory = CreateFactory(destinationOverrides: new Dictionary<string, string?>
        {
            ["ReverseProxy:Clusters:interest:Destinations:destination1:Address"] = stub.Address,
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/interest/_probe/ping");

        (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed)
            .Should().BeTrue($"expected 404 or 405, got {(int)response.StatusCode}");
        stub.ReceivedAnyRequest.Should().BeFalse();
    }

    [Fact]
    public async Task InventoryPath_Returns404_BecauseThePublicEdgeHasNoRouteForIt()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/inventory/anything");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CatalogPath_WithAMethodOutsideTheAllowedList_NeverReachesADestination()
    {
        await using var stub = await DestinationStub.StartAsync();
        await using var factory = CreateFactory(destinationOverrides: new Dictionary<string, string?>
        {
            ["ReverseProxy:Clusters:catalog:Destinations:destination1:Address"] = stub.Address,
        });
        using var client = factory.CreateClient();

        // catalog-read only allows GET/HEAD/OPTIONS. POST matches no endpoint, so routing itself
        // rejects it — YARP never gets to select a destination.
        var response = await client.PostAsync("/api/catalog/_probe/ping", content: null);

        (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed)
            .Should().BeTrue($"expected 404 or 405, got {(int)response.StatusCode}");
        stub.ReceivedAnyRequest.Should().BeFalse();
    }

    private static WebApplicationFactory<GatewayPublic::Program> CreateFactory(
        IDictionary<string, string?>? destinationOverrides = null) =>
        new WebApplicationFactory<GatewayPublic::Program>().WithWebHostBuilder(builder =>
        {
            // appsettings.Development.json is what carries Cors:AllowedOrigins and the real
            // (but here overridden) ReverseProxy Destinations — see CorsSettingsValidator, which
            // fails ValidateOnStart on an empty allow-list in any other environment.
            builder.UseEnvironment("Development");

            if (destinationOverrides is { Count: > 0 })
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(destinationOverrides));
            }
        });
}
