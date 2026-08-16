using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using SbaCars.BuildingBlocks.Web.Auth;

namespace SbaCars.BuildingBlocks.Observability.Tests;

/// <summary>
/// Proves the three <see cref="SbaCarsHealthChecksExtensions"/> endpoints (§8): each answers only
/// with the checks carrying its own <see cref="HealthCheckTags"/> value, and all three stay
/// reachable even when the host's default policy would otherwise require authentication — the
/// same default-deny <c>FallbackPolicy</c> A6 registers on every real service.
/// </summary>
public sealed class HealthChecksEndpointTests
{
    [Fact]
    public async Task Live_ReportsHealthy_FromTheSelfCheckAlone()
    {
        await using var app = await StartAsync();
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ready_ReflectsAFailingDependencyCheck_As503()
    {
        // AddHealthChecks(), not AddSbaCarsHealthChecks(): StartAsync already registered the
        // self check via AddSbaCarsHealthChecks() — calling it a second time here would register
        // "self" twice and fail the host at boot with a duplicate-registration error.
        await using var app = await StartAsync(configureBuilder: builder =>
            builder.Services.AddHealthChecks()
                .AddCheck("fake-dependency", () => HealthCheckResult.Unhealthy(), tags: [HealthCheckTags.Ready]));
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Ready_IgnoresAFailingCheckThatIsOnlyTaggedLive()
    {
        // A check tagged only "live" must never surface on /health/ready — that would make a
        // process report "not ready" for a reason /health/live already answers on its own.
        // AddHealthChecks(), not AddSbaCarsHealthChecks() — see the comment in
        // Ready_ReflectsAFailingDependencyCheck_As503 above.
        await using var app = await StartAsync(configureBuilder: builder =>
            builder.Services.AddHealthChecks()
                .AddCheck("fake-live-only", () => HealthCheckResult.Unhealthy(), tags: [HealthCheckTags.Live]));
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AllThreeEndpoints_AreReachable_UnderTheRealDefaultDenyFallbackPolicy()
    {
        // The exact FallbackPolicy AddSbaCarsAuth registers on every real service (A6, §5.6): no
        // token presented at all. A sibling endpoint with no [AllowAnonymous] gets 401 from it —
        // proving the policy is genuinely active — while the three health endpoints must still be
        // 200, proving MapSbaCarsHealthChecks' AllowAnonymous really overrides it.
        await using var app = await StartAsync(
            configureBuilder: builder =>
            {
                builder.Services.AddSbaCarsAuth(builder.Configuration, builder.Environment);

                // Swaps Logto discovery for nothing, same technique as
                // SbaCars.BuildingBlocks.Web.Tests.AuthorizationTests: no token is ever presented
                // in this test, so no discovery document is ever needed — this just stops
                // JwtBearer from trying to reach a real Logto at options-bind time.
                builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = null;
                    options.RequireHttpsMetadata = false;
                });
            },
            configureApp: app =>
            {
                app.UseSbaCarsAuth();
                app.MapGet("/protected", () => Results.Ok());
            });
        using var client = app.GetTestClient();

        (await client.GetAsync("/protected")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        foreach (var path in new[] { "/health/live", "/health/ready", "/health/startup" })
        {
            var response = await client.GetAsync(path);
            response.StatusCode.Should().Be(HttpStatusCode.OK, because: $"{path} must be anonymous");
        }
    }

    private static async Task<WebApplication> StartAsync(
        Action<WebApplicationBuilder>? configureBuilder = null,
        Action<WebApplication>? configureApp = null)
    {
        // Development, not the CreateBuilder() default (which resolves to "Production" absent an
        // ASPNETCORE_ENVIRONMENT override): AddSbaCarsAuth ties RequireHttpsMetadata to
        // environment.IsDevelopment(), and the PostConfigure a test uses to neutralize Logto
        // discovery runs after AddSbaCarsAuth's own Configure step, not before — outside
        // Development it would otherwise reach JwtBearer's own HTTPS-required check on an HTTP
        // Authority before the neutralizing PostConfigure ever ran.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        builder.Services.AddSbaCarsHealthChecks();
        configureBuilder?.Invoke(builder);

        var app = builder.Build();
        // configureApp runs before MapSbaCarsHealthChecks so a test can insert auth middleware
        // ahead of it, matching the real Program.cs pipeline order (UseSbaCarsAuth before the
        // health endpoints are mapped).
        configureApp?.Invoke(app);
        app.MapSbaCarsHealthChecks();

        await app.StartAsync();
        return app;
    }
}
