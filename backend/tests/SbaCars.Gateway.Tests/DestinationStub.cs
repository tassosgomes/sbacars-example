using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SbaCars.Gateway.Tests;

/// <summary>
/// A minimal in-process HTTP destination — Kestrel bound to an ephemeral port — standing in for
/// a real service behind a gateway. Its address is injected into a gateway's
/// <c>ReverseProxy:Clusters:*:Destinations</c> configuration via
/// <see cref="Microsoft.Extensions.Configuration.MemoryConfigurationBuilderExtensions.AddInMemoryCollection"/>
/// on top of the gateway's own <c>appsettings.Development.json</c>, so YARP proxies to this
/// process instead of a real service. Records the last request it received, which is how the
/// tests prove both that a rewritten path lands correctly and that a rejected-at-the-edge request
/// never gets this far.
/// </summary>
internal sealed class DestinationStub : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly RequestLog _log;

    private DestinationStub(WebApplication app, RequestLog log, string address)
    {
        _app = app;
        _log = log;
        Address = address;
    }

    /// <summary>The base address (<c>http://127.0.0.1:{port}</c>) this stub is listening on.</summary>
    public string Address { get; }

    /// <summary>Whether any request reached this stub since it started.</summary>
    public bool ReceivedAnyRequest => _log.ReceivedAnyRequest;

    /// <summary>The path of the last request this stub received, exactly as YARP forwarded it.</summary>
    public string? LastRequestPath => _log.LastRequestPath;

    public static async Task<DestinationStub> StartAsync()
    {
        var log = new RequestLog();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();

        var app = builder.Build();
        app.Run(context =>
        {
            log.Record(context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status200OK;
            return context.Response.WriteAsync("stub-ok");
        });

        await app.StartAsync();

        return new DestinationStub(app, log, app.Urls.Single());
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();

    private sealed class RequestLog
    {
        private volatile string? _lastRequestPath;

        public bool ReceivedAnyRequest => _lastRequestPath is not null;

        public string? LastRequestPath => _lastRequestPath;

        public void Record(PathString path) => _lastRequestPath = path.Value;
    }
}
