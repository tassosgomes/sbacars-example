using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;

namespace SbaCars.BuildingBlocks.Web.Runtime;

/// <summary>
/// Reads <c>X-Forwarded-For</c>/<c>X-Forwarded-Proto</c> so a service sees the real client scheme
/// and address instead of the gateway's (§8, "Runtime" row) — called only by the four services,
/// which "ficam atrás dos gateways" by design (§2.3); never by a gateway itself, which is the
/// outermost .NET process in this topology and has no upstream proxy of ours to trust locally.
/// </summary>
/// <remarks>
/// <para>
/// <b>Closes half of the pending note in <c>RateLimitingExtensions.GetClientIdentifier</c>.</b>
/// That comment flags "behind a reverse proxy or load balancer this would need
/// <c>ForwardedHeadersMiddleware</c> configured first" — this is that middleware, wired into the
/// four services it actually applies to. It does not touch <c>gateway-public</c>'s own rate
/// limiter, though: <c>gateway-public</c> is read by real client IPs directly in every environment
/// this repository runs (local compose has no further LB in front of it), and the Swarm-level edge
/// load balancer/TLS termination §11.2 describes is Fase D, not this one — so the other half of
/// that comment (gateway-public itself sitting behind *another* proxy in Swarm) stays open,
/// explicitly, for D2.
/// </para>
/// <para>
/// <see cref="ForwardedHeadersOptions.KnownIPNetworks"/>/<see cref="ForwardedHeadersOptions.KnownProxies"/>
/// are cleared rather than populated: ASP.NET Core's default is to trust only loopback, which would
/// silently ignore every forwarded header from a gateway running in a different container. Trusting
/// any upstream unconditionally is safe here specifically because the trust boundary is the network,
/// not this list (§11.2) — the three services are reachable only through the gateways (by Docker
/// network topology locally, by the Swarm overlay in production); nothing else can ever be the
/// upstream hop these headers claim to be from.
/// </para>
/// </remarks>
public static class ForwardedHeadersExtensions
{
    public static IServiceCollection AddSbaCarsForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    /// <summary>
    /// Applies the forwarded-headers middleware. Must run before anything that reads
    /// <c>HttpContext.Connection.RemoteIpAddress</c> or <c>Request.Scheme</c> — first in the
    /// pipeline, ahead of even <c>UseExceptionHandler</c>.
    /// </summary>
    public static WebApplication UseSbaCarsForwardedHeaders(this WebApplication app)
    {
        app.UseForwardedHeaders();
        return app;
    }
}
