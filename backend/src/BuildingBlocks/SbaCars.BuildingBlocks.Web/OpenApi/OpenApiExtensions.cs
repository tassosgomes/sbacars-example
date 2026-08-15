using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace SbaCars.BuildingBlocks.Web.OpenApi;

/// <summary>
/// Native .NET 10 OpenAPI document generation (<c>Microsoft.AspNetCore.OpenApi</c>), plus an
/// interactive UI (Scalar) gated to non-production environments. Only the four
/// <c>SbaCars.&lt;Service&gt;.Api</c> hosts call this — the gateways route, they do not describe
/// an API surface of their own.
/// </summary>
/// <remarks>
/// <b>UI choice: Scalar, not Swagger UI.</b> Scalar renders directly from the document produced
/// by <c>Microsoft.AspNetCore.OpenApi</c> with a single <c>MapScalarApiReference()</c> call and
/// no separate generation pipeline of its own. Swashbuckle's Swagger UI is built around
/// Swashbuckle's own generator, which would mean running two OpenAPI generators side by side (or
/// replacing the native one) for no benefit here. Scalar is MIT-licensed and actively
/// maintained, and is the pairing generally recommended alongside .NET's built-in OpenAPI
/// support.
/// </remarks>
public static class OpenApiExtensions
{
    public static IServiceCollection AddSbaCarsOpenApi(this IServiceCollection services) =>
        services.AddOpenApi();

    /// <summary>
    /// Maps the OpenAPI document endpoint (always available — a future CI gate can diff it
    /// regardless of environment) and, outside of Production, the interactive Scalar UI.
    /// </summary>
    public static WebApplication UseSbaCarsOpenApi(this WebApplication app)
    {
        app.MapOpenApi();

        if (!app.Environment.IsProduction())
        {
            app.MapScalarApiReference();
        }

        return app;
    }
}
