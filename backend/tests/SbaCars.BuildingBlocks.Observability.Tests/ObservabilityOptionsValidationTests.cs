using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SbaCars.BuildingBlocks.Observability.Tests;

/// <summary>
/// Proves the boot-time behavior <c>ObservabilityExtensions.AddSbaCarsObservability</c>'s remarks
/// describe (§4.4, §8): a malformed <c>Observability:OtlpEndpoint</c> fails the host at boot, an
/// absent one does not — that split is deliberate, not an oversight (see the remarks on
/// <c>AddSbaCarsObservability</c> for why).
/// </summary>
public sealed class ObservabilityOptionsValidationTests
{
    [Fact]
    public async Task MalformedOtlpEndpoint_PreventsTheHostFromStarting()
    {
        var act = () => TestHostFactory.StartAsync(
            configuration: new Dictionary<string, string?>
            {
                ["Observability:OtlpEndpoint"] = "not-a-uri",
            },
            configureBuilder: builder =>
                builder.Services.AddSbaCarsObservability(builder.Configuration, "test-service"));

        await act.Should().ThrowAsync<OptionsValidationException>();
    }

    [Fact]
    public async Task MissingOtlpEndpoint_StillLetsTheHostStart()
    {
        // The exact "no dashboard running in Development" case AddSbaCarsObservability's remarks
        // describe: instrumentation stays wired, exporters are simply never added.
        await using var app = await TestHostFactory.StartAsync(
            configureBuilder: builder =>
                builder.Services.AddSbaCarsObservability(builder.Configuration, "test-service"));

        app.Services.GetRequiredService<IOptions<ObservabilitySettings>>().Value.OtlpEndpoint
            .Should().BeNull();
    }

    [Fact]
    public async Task ValidOtlpEndpoint_LetsTheHostStart()
    {
        await using var app = await TestHostFactory.StartAsync(
            configuration: new Dictionary<string, string?>
            {
                ["Observability:OtlpEndpoint"] = "http://localhost:18889",
            },
            configureBuilder: builder =>
                builder.Services.AddSbaCarsObservability(builder.Configuration, "test-service"));

        app.Services.GetRequiredService<IOptions<ObservabilitySettings>>().Value.OtlpEndpoint
            .Should().Be("http://localhost:18889");
    }
}
