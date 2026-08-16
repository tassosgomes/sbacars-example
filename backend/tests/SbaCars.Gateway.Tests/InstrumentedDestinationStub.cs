using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using SbaCars.BuildingBlocks.Observability;

namespace SbaCars.Gateway.Tests;

/// <summary>
/// A <see cref="DestinationStub"/> that is also wired with the real
/// <c>AddSbaCarsObservability</c> — the exact same shared extension every one of the four
/// services calls — so a gateway proxying to it exercises the real OpenTelemetry ASP.NET Core
/// instrumentation on the downstream side, not a bare 200-OK stand-in. An in-memory exporter
/// captures every span this stub's own request pipeline ends, which is how
/// <see cref="TraceContinuityTests"/> proves the trace-id (and correct parent span) survives the
/// hop from gateway to service without needing to boot a full real service host (Postgres, Logto)
/// just to observe trace propagation, a mechanism that lives entirely in
/// <c>BuildingBlocks.Observability</c> and ASP.NET Core itself.
/// </summary>
internal sealed class InstrumentedDestinationStub : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly List<Activity> _exportedActivities;

    private InstrumentedDestinationStub(WebApplication app, List<Activity> exportedActivities, string address)
    {
        _app = app;
        _exportedActivities = exportedActivities;
        Address = address;
    }

    /// <summary>The base address (<c>http://127.0.0.1:{port}</c>) this stub is listening on.</summary>
    public string Address { get; }

    /// <summary>
    /// Every span this stub's tracer provider has ended and exported so far — synchronous, thanks
    /// to the in-memory exporter's <c>SimpleActivityExportProcessor</c>, so it is safe to read
    /// immediately after the request that produced it completes.
    /// </summary>
    public IReadOnlyList<Activity> ExportedActivities => _exportedActivities;

    public static async Task<InstrumentedDestinationStub> StartAsync(string serviceName = "catalog-service-stub")
    {
        var exportedActivities = new List<Activity>();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();

        // No Observability:OtlpEndpoint configured — proves the same "boots and traces without a
        // dashboard" claim ObservabilityExtensions.AddSbaCarsObservability's remarks make, and
        // keeps this test independent of any real collector.
        builder.Services.AddSbaCarsObservability(builder.Configuration, serviceName);
        builder.Services.ConfigureOpenTelemetryTracerProvider((_, tracing) =>
            tracing.AddInMemoryExporter(exportedActivities));

        var app = builder.Build();
        app.MapGet("/api/_probe/ping", () => Results.Ok());

        await app.StartAsync();

        return new InstrumentedDestinationStub(app, exportedActivities, app.Urls.Single());
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();
}
