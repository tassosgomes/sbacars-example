using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SbaCars.BuildingBlocks.Web.Auth;

/// <summary>Registers <see cref="JwksReadinessHealthCheck"/> — see its own remarks for what it proves.</summary>
public static class JwksHealthCheckExtensions
{
    /// <summary>
    /// Adds the JWKS readiness check named <c>"jwks"</c>, tagged with whatever the caller passes —
    /// in practice always <c>SbaCars.BuildingBlocks.Observability.HealthCheckTags.Ready</c> (kept
    /// as a plain parameter for the same reason as <c>AddSbaCarsPostgresReadinessCheck</c>: this
    /// project should not need a reference to <c>BuildingBlocks.Observability</c> just to read one
    /// tag name). Only ever called where <see cref="AuthExtensions.AddSbaCarsAuth"/> was also
    /// called — a process with no authentication has no JWKS dependency to check.
    /// </summary>
    public static IHealthChecksBuilder AddSbaCarsJwksReadinessCheck(
        this IHealthChecksBuilder builder,
        params string[] tags) =>
        builder.AddCheck<JwksReadinessHealthCheck>("jwks", tags: tags);
}
