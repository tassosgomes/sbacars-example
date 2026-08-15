using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SbaCars.BuildingBlocks.Web.Cors;

/// <summary>
/// A single named CORS policy, sourced entirely from configuration. Only the two gateways call
/// this — CORS exists at the edge, never inside a domain service (§2.3 of the architecture plan).
/// </summary>
public static class CorsExtensions
{
    /// <summary>
    /// Name of the policy registered by <see cref="AddSbaCarsCors"/>, for use with
    /// <see cref="UseSbaCarsCors"/> or an explicit <c>[EnableCors]</c>/<c>RequireCors</c> call.
    /// </summary>
    public const string PolicyName = "SbaCars";

    /// <summary>
    /// Binds and validates <see cref="CorsSettings"/>, then registers a CORS policy built from
    /// it. Credentials are never allowed: authentication is bearer-token based, not cookie based
    /// (§5.5), so there is nothing a credentialed CORS policy would protect here — only risk it
    /// would add.
    /// </summary>
    public static IServiceCollection AddSbaCarsCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CorsSettings>()
            .Bind(configuration.GetSection(CorsSettings.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<CorsSettings>, CorsSettingsValidator>();

        services.AddCors();

        // The CorsOptions the middleware reads at request time is configured lazily from the
        // validated CorsSettings, so the policy always reflects whatever ValidateOnStart let
        // through — never a value read once, ahead of validation, at registration time.
        services.AddOptions<CorsOptions>()
            .Configure<IOptions<CorsSettings>>((corsOptions, settings) =>
            {
                corsOptions.AddPolicy(PolicyName, policy => policy
                    .WithOrigins(settings.Value.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod());
            });

        return services;
    }

    /// <summary>
    /// Applies the policy registered by <see cref="AddSbaCarsCors"/>.
    /// </summary>
    public static IApplicationBuilder UseSbaCarsCors(this IApplicationBuilder app) =>
        app.UseCors(PolicyName);
}
