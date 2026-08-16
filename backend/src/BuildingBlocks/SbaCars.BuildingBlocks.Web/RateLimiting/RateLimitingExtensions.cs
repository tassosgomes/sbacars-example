using System.Diagnostics;
using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SbaCars.BuildingBlocks.Web.RateLimiting;

/// <summary>
/// A sliding-window rate limiter, partitioned per client IP, using ASP.NET Core's native
/// <c>System.Threading.RateLimiting</c>. Only <c>gateway-public</c> wires this: it is the one
/// process that accepts anonymous traffic from the internet (§5.5 of the architecture plan).
/// </summary>
/// <remarks>
/// <para>
/// <b>In-memory limitation — read before adding a second replica.</b> Every partition here lives
/// in this process's memory. With <c>N</c> replicas behind the gateway's routing mesh, an
/// attacker (or just bad luck in load balancing) can receive up to <c>N</c> times the configured
/// limit, because each replica counts independently and none of them know about the others. This
/// is called out explicitly in §11.7 of the architecture plan: a distributed counter (Redis) is
/// task D6, not this one. The fix, when D6 lands, is scoped to the two <c>factory</c> lambdas
/// below — swap <see cref="RateLimitPartition.GetSlidingWindowLimiter{TKey}"/> for a
/// Redis-backed <see cref="RateLimiter"/> keyed the same way. Policy names
/// (<see cref="RateLimitingPolicies"/>), the options shape, and every call site that references
/// them by name stay exactly as they are — that is the seam this design leaves ready.
/// </para>
/// </remarks>
public static class RateLimitingExtensions
{
    public static IServiceCollection AddSbaCarsRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitingSettings>()
            .Bind(configuration.GetSection(RateLimitingSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Applies to every request that reaches this host — the baseline protection every
            // anonymous endpoint gets for free, with no per-route opt-in required.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var settings = httpContext.RequestServices
                    .GetRequiredService<IOptions<RateLimitingSettings>>().Value;

                return RateLimitPartition.GetSlidingWindowLimiter(
                    GetClientIdentifier(httpContext),
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                        SegmentsPerWindow = settings.SegmentsPerWindow,
                        QueueLimit = settings.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    });
            });

            // Opt-in, stricter window for specific anonymous write endpoints
            // (`.RequireRateLimiting(RateLimitingPolicies.AnonymousStrict)`). Stacks on top of
            // the global limiter above — both must allow the request through.
            options.AddPolicy(RateLimitingPolicies.AnonymousStrict, httpContext =>
            {
                var settings = httpContext.RequestServices
                    .GetRequiredService<IOptions<RateLimitingSettings>>().Value;

                return RateLimitPartition.GetSlidingWindowLimiter(
                    GetClientIdentifier(httpContext),
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.AnonymousStrictPermitLimit,
                        Window = TimeSpan.FromSeconds(settings.AnonymousStrictWindowSeconds),
                        SegmentsPerWindow = settings.SegmentsPerWindow,
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        retryAfter.TotalSeconds.ToString(CultureInfo.InvariantCulture);
                }

                var traceId = Activity.Current?.TraceId.ToHexString() ?? context.HttpContext.TraceIdentifier;

                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new
                    {
                        type = $"https://httpstatuses.io/{StatusCodes.Status429TooManyRequests}",
                        title = "Too many requests.",
                        status = StatusCodes.Status429TooManyRequests,
                        detail = "Rate limit exceeded. Try again later.",
                        instance = context.HttpContext.Request.Path.Value,
                        traceId,
                    },
                    cancellationToken);
            };
        });

        return services;
    }

    /// <summary>
    /// Applies the rate limiting middleware registered by <see cref="AddSbaCarsRateLimiting"/>.
    /// </summary>
    public static IApplicationBuilder UseSbaCarsRateLimiting(this IApplicationBuilder app) =>
        app.UseRateLimiter();

    // NOTE: this reads the socket's remote address, not a `Forwarded`/`X-Forwarded-For` header.
    // Behind a reverse proxy or load balancer this would need `ForwardedHeadersMiddleware`
    // configured first (§8, "Runtime" row). A8 wired that middleware
    // (`SbaCars.BuildingBlocks.Web.Runtime.ForwardedHeadersExtensions`) — but only for the four
    // services behind these two gateways, not for gateway-public itself. This limiter only ever
    // runs in gateway-public, which is the outermost .NET process in every environment this
    // repository currently runs (local compose has no further LB in front of it), so there is no
    // upstream of ours to trust yet. The day gateway-public sits behind Swarm's own edge load
    // balancer/TLS termination (§11.2, Fase D2), this is the seam to revisit: wire
    // ForwardedHeaders here too and swap `httpContext.Connection.RemoteIpAddress` below for the
    // header it resolves.
    private static string GetClientIdentifier(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
