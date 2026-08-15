using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SbaCars.BuildingBlocks.Web.Cors;
using SbaCars.BuildingBlocks.Web.RateLimiting;

namespace SbaCars.BuildingBlocks.Web.Tests;

/// <summary>
/// Proves §4.4 of the architecture plan for the two configuration sections this task owns:
/// invalid or missing configuration must fail the host at boot (<c>ValidateOnStart</c>), never
/// surface as a confusing failure on the first request.
/// </summary>
public sealed class OptionsValidationTests
{
    [Fact]
    public async Task MissingCorsAllowedOrigins_PreventsTheHostFromStarting()
    {
        var act = () => TestHostFactory.StartAsync(
            configureBuilder: builder => builder.Services.AddSbaCarsCors(builder.Configuration));

        await act.Should().ThrowAsync<OptionsValidationException>();
    }

    [Fact]
    public async Task InvalidCorsOrigin_PreventsTheHostFromStarting()
    {
        var act = () => TestHostFactory.StartAsync(
            configuration: new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "not-a-valid-origin",
            },
            configureBuilder: builder => builder.Services.AddSbaCarsCors(builder.Configuration));

        await act.Should().ThrowAsync<OptionsValidationException>();
    }

    [Fact]
    public async Task InvalidRateLimitingConfiguration_PreventsTheHostFromStarting()
    {
        var act = () => TestHostFactory.StartAsync(
            configuration: new Dictionary<string, string?>
            {
                // Zero is outside the [Range(1, int.MaxValue)] the setting declares.
                ["RateLimiting:PermitLimit"] = "0",
            },
            configureBuilder: builder => builder.Services.AddSbaCarsRateLimiting(builder.Configuration));

        await act.Should().ThrowAsync<OptionsValidationException>();
    }

    [Fact]
    public async Task ValidConfiguration_LetsTheHostStart()
    {
        await using var app = await TestHostFactory.StartAsync(
            configuration: new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
            },
            configureBuilder: builder =>
            {
                builder.Services.AddSbaCarsCors(builder.Configuration);
                builder.Services.AddSbaCarsRateLimiting(builder.Configuration);
            });

        // Reaching this line without throwing already answers the question; the assertion is a
        // second, explicit check on the values ValidateOnStart just accepted.
        app.Services.GetRequiredService<IOptions<CorsSettings>>().Value.AllowedOrigins
            .Should().Contain("http://localhost:5173");
        app.Services.GetRequiredService<IOptions<RateLimitingSettings>>().Value.PermitLimit
            .Should().Be(100);
    }
}
