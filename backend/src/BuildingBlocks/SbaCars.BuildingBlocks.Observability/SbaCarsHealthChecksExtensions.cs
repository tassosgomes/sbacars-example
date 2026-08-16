using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SbaCars.BuildingBlocks.Observability;

/// <summary>
/// Maps the three health endpoints every process in the topology exposes (§8): <c>/health/live</c>
/// (self, no dependency), <c>/health/ready</c> (real dependencies — Postgres and JWKS for the four
/// services, downstream service readiness for a gateway) and <c>/health/startup</c>. A check lands
/// on the right endpoint purely by carrying the matching <see cref="HealthCheckTags"/> value;
/// callers never touch <c>MapHealthChecks</c> directly.
/// </summary>
public static class SbaCarsHealthChecksExtensions
{
    /// <summary>
    /// Registers the health check service plus the one check every process needs and none can be
    /// missing — a trivial self check, tagged both <see cref="HealthCheckTags.Live"/> and
    /// <see cref="HealthCheckTags.Startup"/>. Real dependency checks (Postgres, JWKS, downstream
    /// services) are added by the caller on the returned builder — each process registers a
    /// different set, so there is nothing generic left to add here beyond "the process itself is
    /// running".
    /// </summary>
    public static IHealthChecksBuilder AddSbaCarsHealthChecks(this IServiceCollection services) =>
        services.AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy(),
                tags: [HealthCheckTags.Live, HealthCheckTags.Startup]);

    /// <summary>
    /// Maps <c>/health/live</c>, <c>/health/ready</c> and <c>/health/startup</c>, each filtered to
    /// the checks carrying its <see cref="HealthCheckTags"/> value. All three are anonymous —
    /// the default-deny <c>FallbackPolicy</c> A6 registers would otherwise block them on any
    /// process that calls <c>AddSbaCarsAuth</c> — and exempt from rate limiting, so an orchestrator
    /// polling <c>gateway-public</c>'s health never competes with the anonymous-surface budget
    /// meant for real traffic (§5.5). <see cref="DisableRateLimiting"/> is a no-op, and therefore
    /// safe to apply unconditionally, on any process that never wired a rate limiter in the first
    /// place.
    /// </summary>
    public static WebApplication MapSbaCarsHealthChecks(this WebApplication app)
    {
        MapTagged(app, "/health/live", HealthCheckTags.Live);
        MapTagged(app, "/health/ready", HealthCheckTags.Ready);
        MapTagged(app, "/health/startup", HealthCheckTags.Startup);
        return app;
    }

    private static void MapTagged(WebApplication app, string pattern, string tag) =>
        app.MapHealthChecks(pattern, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(tag),
            ResponseWriter = WriteResponseAsync,
        })
            .AllowAnonymous()
            .DisableRateLimiting();

    private static Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("status", report.Status.ToString());
            writer.WriteNumber("totalDurationMs", report.TotalDuration.TotalMilliseconds);
            writer.WriteStartArray("checks");
            foreach (var (name, entry) in report.Entries)
            {
                writer.WriteStartObject();
                writer.WriteString("name", name);
                writer.WriteString("status", entry.Status.ToString());
                writer.WriteNumber("durationMs", entry.Duration.TotalMilliseconds);
                if (entry.Description is not null)
                {
                    writer.WriteString("description", entry.Description);
                }

                writer.WriteStartArray("tags");
                foreach (var tag in entry.Tags)
                {
                    writer.WriteStringValue(tag);
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return context.Response.Body.WriteAsync(stream.ToArray()).AsTask();
    }
}
