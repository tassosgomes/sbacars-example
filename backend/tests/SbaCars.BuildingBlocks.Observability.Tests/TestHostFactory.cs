using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SbaCars.BuildingBlocks.Observability.Tests;

/// <summary>
/// Builds a minimal, in-process ASP.NET Core host (<see cref="Microsoft.AspNetCore.TestHost"/>),
/// mirroring <c>SbaCars.BuildingBlocks.Web.Tests.TestHostFactory</c>, so this project can exercise
/// <c>AddSbaCarsObservability</c>/<c>AddSbaCarsHealthChecks</c> without depending on any of the six
/// real service/gateway hosts.
/// </summary>
internal static class TestHostFactory
{
    public static async Task<WebApplication> StartAsync(
        Action<WebApplicationBuilder>? configureBuilder = null,
        Action<WebApplication>? configureApp = null,
        IDictionary<string, string?>? configuration = null,
        string environmentName = "Development")
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName,
        });

        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        if (configuration is not null)
        {
            builder.Configuration.AddInMemoryCollection(configuration);
        }

        configureBuilder?.Invoke(builder);

        var app = builder.Build();

        configureApp?.Invoke(app);

        await app.StartAsync();
        return app;
    }
}
