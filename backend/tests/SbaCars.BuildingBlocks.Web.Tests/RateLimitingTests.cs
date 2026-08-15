using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using SbaCars.BuildingBlocks.Web.RateLimiting;

namespace SbaCars.BuildingBlocks.Web.Tests;

public sealed class RateLimitingTests
{
    [Fact]
    public async Task ExceedingTheAnonymousStrictLimit_ReturnsTooManyRequests()
    {
        await using var app = await TestHostFactory.StartAsync(
            configuration: new Dictionary<string, string?>
            {
                // Keep the general/global policy permissive so only the stricter, endpoint-level
                // policy under test is what trips.
                ["RateLimiting:PermitLimit"] = "1000",
                ["RateLimiting:WindowSeconds"] = "60",
                ["RateLimiting:AnonymousStrictPermitLimit"] = "2",
                ["RateLimiting:AnonymousStrictWindowSeconds"] = "60",
            },
            configureBuilder: builder => builder.Services.AddSbaCarsRateLimiting(builder.Configuration),
            configureApp: app =>
            {
                app.UseSbaCarsRateLimiting();
                app.MapGet("/limited", () => Results.Ok())
                    .RequireRateLimiting(RateLimitingPolicies.AnonymousStrict);
            });

        // TestServer sends every request from the same synthetic client, so all three share the
        // same per-IP partition key.
        using var client = app.GetTestClient();

        var first = await client.GetAsync("/limited");
        var second = await client.GetAsync("/limited");
        var third = await client.GetAsync("/limited");

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        third.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task GlobalLimit_AppliesEvenWithoutAnEndpointSpecificPolicy()
    {
        await using var app = await TestHostFactory.StartAsync(
            configuration: new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = "1",
                ["RateLimiting:WindowSeconds"] = "60",
            },
            configureBuilder: builder => builder.Services.AddSbaCarsRateLimiting(builder.Configuration),
            configureApp: app =>
            {
                app.UseSbaCarsRateLimiting();
                app.MapGet("/unrestricted", () => Results.Ok());
            });

        using var client = app.GetTestClient();

        var first = await client.GetAsync("/unrestricted");
        var second = await client.GetAsync("/unrestricted");

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
