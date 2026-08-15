using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using SbaCars.BuildingBlocks.Web.CorrelationId;

namespace SbaCars.BuildingBlocks.Web.Tests;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task IncomingCorrelationId_IsEchoedBackOnTheResponse()
    {
        await using var app = await CreatePingHost();
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add(CorrelationIdMiddleware.HeaderName, "test-correlation-123");

        var response = await client.GetAsync("/ping");

        response.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var values).Should().BeTrue();
        values.Should().ContainSingle().Which.Should().Be("test-correlation-123");
    }

    [Fact]
    public async Task MissingCorrelationId_IsGeneratedAndReturned()
    {
        await using var app = await CreatePingHost();
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/ping");

        response.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var values).Should().BeTrue();
        var generated = values.Should().ContainSingle().Which;
        generated.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(generated, out _).Should().BeTrue("a generated correlation id is a GUID");
    }

    private static Task<WebApplication> CreatePingHost() =>
        TestHostFactory.StartAsync(
            configureApp: app =>
            {
                app.UseSbaCarsCorrelationId();
                app.MapGet("/ping", () => Results.Ok());
            });
}
