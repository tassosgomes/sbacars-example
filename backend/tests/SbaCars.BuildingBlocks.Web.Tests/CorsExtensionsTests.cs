using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using SbaCars.BuildingBlocks.Web.Cors;

namespace SbaCars.BuildingBlocks.Web.Tests;

public sealed class CorsExtensionsTests
{
    [Fact]
    public async Task ConfiguredOrigin_IsAllowedByThePolicy()
    {
        await using var app = await TestHostFactory.StartAsync(
            configuration: new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
            },
            configureBuilder: builder => builder.Services.AddSbaCarsCors(builder.Configuration),
            configureApp: app =>
            {
                app.UseSbaCarsCors();
                app.MapGet("/ping", () => Results.Ok());
            });

        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/ping");
        request.Headers.Add("Origin", "http://localhost:5173");

        var response = await client.SendAsync(request);

        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var allowOrigin).Should().BeTrue();
        allowOrigin.Should().ContainSingle().Which.Should().Be("http://localhost:5173");
    }

    [Fact]
    public async Task OriginNotInAllowList_IsNotGrantedAccess()
    {
        await using var app = await TestHostFactory.StartAsync(
            configuration: new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
            },
            configureBuilder: builder => builder.Services.AddSbaCarsCors(builder.Configuration),
            configureApp: app =>
            {
                app.UseSbaCarsCors();
                app.MapGet("/ping", () => Results.Ok());
            });

        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/ping");
        request.Headers.Add("Origin", "http://evil.example.com");

        var response = await client.SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }
}
