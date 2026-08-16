extern alias GatewayPublic;

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SbaCars.Gateway.Tests;

/// <summary>
/// Proves §8's "gateways agregam" line for <c>gateway-public</c>:
/// <c>DownstreamReadinessHealthCheck</c> reads the same YARP cluster map
/// <c>ReverseProxyExtensions</c> already validates at boot (A7) — no separate, hand-maintained
/// list of the services this gateway routes to — and the gateway's own <c>/health/ready</c>
/// reflects whatever that destination reports on its own <c>/health/ready</c>.
/// </summary>
public sealed class GatewayHealthAggregationTests
{
    [Fact]
    public async Task Ready_IsHealthy_WhenTheRoutedServiceReportsReady()
    {
        await using var destination = await StartHealthStubAsync(HttpStatusCode.OK);
        await using var factory = CreateFactory(destination.Address);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ready_Is503_WhenTheRoutedServiceReportsNotReady()
    {
        await using var destination = await StartHealthStubAsync(HttpStatusCode.ServiceUnavailable);
        await using var factory = CreateFactory(destination.Address);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Live_NeverConsultsTheRoutedServices_AndStaysHealthyRegardlessOfTheirState()
    {
        // /health/live is self-only (§8) — a downstream outage must never take the gateway's own
        // liveness down with it, or an orchestrator would restart a perfectly healthy process.
        await using var destination = await StartHealthStubAsync(HttpStatusCode.ServiceUnavailable);
        await using var factory = CreateFactory(destination.Address);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static WebApplicationFactory<GatewayPublic::Program> CreateFactory(string destinationAddress) =>
        new WebApplicationFactory<GatewayPublic::Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ReverseProxy:Clusters:catalog:Destinations:destination1:Address"] = destinationAddress,
                    // gateway-public also routes to "interest" (§2.3) — DownstreamReadinessHealthCheck
                    // walks every cluster, so it needs a destination for this one too, even though
                    // this test only cares about "catalog"'s state.
                    ["ReverseProxy:Clusters:interest:Destinations:destination1:Address"] = destinationAddress,
                }));
        });

    private static async Task<HealthStub> StartHealthStubAsync(HttpStatusCode readyStatusCode)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();

        var app = builder.Build();
        app.MapGet("/health/ready", () => Results.StatusCode((int)readyStatusCode));

        await app.StartAsync();
        return new HealthStub(app);
    }

    private sealed class HealthStub(WebApplication app) : IAsyncDisposable
    {
        public string Address { get; } = app.Urls.Single();

        public async ValueTask DisposeAsync() => await app.DisposeAsync();
    }
}
