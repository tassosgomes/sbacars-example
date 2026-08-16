using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace SbaCars.BuildingBlocks.Web.Auth;

/// <summary>
/// The JWKS leg of <c>/health/ready</c> (§8), for every process that calls <see cref="AuthExtensions.AddSbaCarsAuth"/>
/// (the four services and <c>gateway-backoffice</c> — never <c>gateway-public</c>, which never
/// wires authentication at all, §5.5). Reuses the exact <see cref="JwtBearerOptions.ConfigurationManager"/>
/// <c>AddJwtBearer</c> itself builds and caches — the same discovery-document-plus-JWKS fetch a
/// real token validation would trigger — rather than standing up a second, parallel HTTP call to
/// the metadata address. A green check means "the mechanism token validation actually depends on
/// works right now", not just "some URL responds".
/// </summary>
public sealed class JwksReadinessHealthCheck(IOptionsMonitor<JwtBearerOptions> jwtBearerOptions) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var configurationManager = jwtBearerOptions.Get(JwtBearerDefaults.AuthenticationScheme).ConfigurationManager;
        if (configurationManager is null)
        {
            return HealthCheckResult.Unhealthy("JwtBearerOptions.ConfigurationManager was never configured.");
        }

        try
        {
            var configuration = await configurationManager.GetConfigurationAsync(cancellationToken);
            return configuration.JsonWebKeySet is { Keys.Count: > 0 }
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Logto's discovery document resolved but its JWKS has no keys.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Could not resolve Logto's JWKS discovery document.", ex);
        }
    }
}
