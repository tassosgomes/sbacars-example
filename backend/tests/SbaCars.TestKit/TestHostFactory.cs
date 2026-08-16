using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SbaCars.TestKit;

/// <summary>
/// Builds a minimal, in-process ASP.NET Core host (<see cref="Microsoft.AspNetCore.TestHost"/>) so
/// middleware and handlers from any <c>BuildingBlocks</c> library can be exercised end to end
/// without depending on any of the six real service/gateway hosts.
/// </summary>
/// <remarks>
/// Consolidated into <c>SbaCars.TestKit</c> in A9: this factory used to be duplicated, byte for
/// byte except namespace and comment, between <c>SbaCars.BuildingBlocks.Web.Tests</c> and
/// <c>SbaCars.BuildingBlocks.Observability.Tests</c> — the second consumer that justifies
/// extracting shared test code under §3.3's own rule ("extrai no segundo consumidor real").
/// </remarks>
public static class TestHostFactory
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
