using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace SbaCars.BuildingBlocks.Web.Runtime;

/// <summary>
/// Request timeouts and graceful shutdown (§8, "Runtime" row) — called by every one of the six
/// processes, the one Runtime concern that does not differ between a service and a gateway.
/// <see cref="ForwardedHeadersExtensions"/> is the other half of that row and is deliberately
/// separate: it applies only to the four services (§8), not here.
/// </summary>
public static class RuntimeExtensions
{
    public static IServiceCollection AddSbaCarsRuntimeReadiness(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RuntimeSettings>()
            .Bind(configuration.GetSection(RuntimeSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRequestTimeouts();
        services.AddOptions<RequestTimeoutOptions>()
            .Configure<IOptions<RuntimeSettings>>((requestTimeouts, runtime) =>
                requestTimeouts.DefaultPolicy = new RequestTimeoutPolicy
                {
                    Timeout = TimeSpan.FromSeconds(runtime.Value.RequestTimeoutSeconds),
                });

        services.AddOptions<HostOptions>()
            .Configure<IOptions<RuntimeSettings>>((hostOptions, runtime) =>
                hostOptions.ShutdownTimeout = TimeSpan.FromSeconds(runtime.Value.GracefulShutdownSeconds));

        return services;
    }

    /// <summary>Applies the request timeout middleware registered by <see cref="AddSbaCarsRuntimeReadiness"/>.</summary>
    public static WebApplication UseSbaCarsRequestTimeouts(this WebApplication app)
    {
        app.UseRequestTimeouts();
        return app;
    }
}
