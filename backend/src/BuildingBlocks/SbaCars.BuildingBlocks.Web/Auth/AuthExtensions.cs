using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SbaCars.BuildingBlocks.Application;

namespace SbaCars.BuildingBlocks.Web.Auth;

/// <summary>
/// JWT authentication plus permission-based authorization (§5.2, §5.4 and §5.6 of the
/// architecture plan), wired identically by every service and by <c>gateway-backoffice</c>.
/// <c>gateway-backoffice</c> validates the token and rejects at the edge; each service
/// revalidates independently — deliberate redundancy, because a service never trusts the edge
/// for its own authorization. <c>gateway-public</c> never calls this: it is the anonymous
/// surface (§5.5).
/// </summary>
public static class AuthExtensions
{
    /// <summary>
    /// The single audience accepted everywhere. Not configuration: an audience that is the same
    /// string in every environment is one fewer thing that can drift between them (§5.2).
    /// </summary>
    public const string Audience = "https://api.sbacars.app";

    /// <summary>
    /// Registers JWT bearer authentication against Logto, the <see cref="ScopeClaimsTransformation"/>
    /// that turns its <c>scope</c> claim into permission claims, one authorization policy per
    /// permission in <see cref="Permissoes.All"/>, a default-deny <c>FallbackPolicy</c>, and the
    /// concrete <see cref="ICurrentUser"/> implementation Application code depends on.
    /// </summary>
    public static IServiceCollection AddSbaCarsAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtSettings>, JwtSettingsValidator>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddTransient<IClaimsTransformation, ScopeClaimsTransformation>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((jwtBearerOptions, jwtSettings) =>
            {
                var settings = jwtSettings.Value;

                // Authority vs. MetadataAddress is the one split that differs between running in
                // a container and running on the host — see JwtSettings.MetadataAddress. Neither
                // knob touches which issuer gets validated: that keeps coming from the discovery
                // document's own `issuer` field, via TokenValidationParameters.ValidIssuer being
                // left unset below.
                jwtBearerOptions.Authority = settings.Authority;
                if (settings.MetadataAddress is not null)
                {
                    jwtBearerOptions.MetadataAddress = settings.MetadataAddress;
                }

                // The ConfigurationManager this sets up under the hood already caches the
                // discovery document and its JWKS, and refreshes both automatically (and
                // immediately, on a signing-key-not-found failure) — key rotation with no extra
                // code here.
                jwtBearerOptions.RequireHttpsMetadata = !environment.IsDevelopment();
                jwtBearerOptions.MapInboundClaims = false;

                jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    // No RoleClaimType configured: authorization here never reads a role claim
                    // (§5.6) — permission claims and PermissionAuthorizationHandler are the only
                    // path from token to policy.
                };
            })
            .ValidateOnStart();

        var authorizationBuilder = services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        foreach (var permission in Permissoes.All)
        {
            authorizationBuilder.AddPolicy(
                permission,
                policy => policy.AddRequirements(new PermissionRequirement(permission)));
        }

        return services;
    }

    /// <summary>Applies authentication then authorization middleware, in that order.</summary>
    public static WebApplication UseSbaCarsAuth(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}
