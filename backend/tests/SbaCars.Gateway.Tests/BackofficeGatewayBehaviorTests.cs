extern alias GatewayBackoffice;

using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SbaCars.Gateway.Tests;

/// <summary>
/// Drives gateway-backoffice end to end through <see cref="WebApplicationFactory{TEntryPoint}"/>,
/// proving the edge-level rejection from §5.2: an unauthenticated request never reaches the
/// destination, because <c>UseSbaCarsAuth</c> runs before <c>MapReverseProxy</c>.
/// </summary>
public sealed class BackofficeGatewayBehaviorTests
{
    [Fact]
    public async Task InventoryPath_WithoutAToken_Returns401AtTheGateway_AndNeverReachesTheDestination()
    {
        await using var stub = await DestinationStub.StartAsync();
        await using var factory = new WebApplicationFactory<GatewayBackoffice::Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ReverseProxy:Clusters:inventory:Destinations:destination1:Address"] = stub.Address,
                    }));
                builder.ConfigureServices(services =>
                {
                    // This test never presents a token, so no discovery document is ever needed
                    // to prove the 401 — but a still-configured Authority could make JwtBearer try
                    // to reach a real Logto anyway. Same technique as
                    // SbaCars.BuildingBlocks.Web.Tests.AuthorizationTests: neutralize Authority so
                    // the test never depends on network access. Runs after AddSbaCarsAuth's own
                    // Configure<JwtBearerOptions>, so it wins.
                    services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                    {
                        options.Authority = null;
                        options.RequireHttpsMetadata = false;
                    });
                });
            });

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/inventory/anything");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        stub.ReceivedAnyRequest.Should().BeFalse();
    }
}
