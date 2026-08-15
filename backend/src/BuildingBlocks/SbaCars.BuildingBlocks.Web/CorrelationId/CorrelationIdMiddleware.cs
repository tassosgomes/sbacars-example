using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace SbaCars.BuildingBlocks.Web.CorrelationId;

/// <summary>
/// Accepts a caller-supplied correlation id from the request header, generating one when it is
/// absent, echoes it back on the response header, and pushes it into the structured logging
/// scope for the rest of the request.
/// </summary>
/// <remarks>
/// This complements, and never replaces, the W3C <c>traceparent</c> propagated end to end by
/// OpenTelemetry (§8 of the architecture plan): the trace id is what stitches spans together
/// across processes, while the correlation id is a caller-controlled token a client or another
/// team can hand back when opening a support ticket, without needing to know anything about
/// tracing.
/// </remarks>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    /// <summary>
    /// The header carrying the correlation id, both on the way in and on the way out.
    /// </summary>
    public const string HeaderName = "X-Correlation-ID";

    /// <summary>
    /// Key under which the resolved correlation id is stashed in <see cref="HttpContext.Items"/>
    /// for the rest of the request, e.g. for <see cref="ErrorHandling.GlobalExceptionHandler"/>
    /// to include it in its error log line.
    /// </summary>
    public const string HttpContextItemKey = "SbaCars.CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Items[HttpContextItemKey] = correlationId;

        context.Response.OnStarting(static state =>
        {
            var (response, correlationId) = ((HttpResponse Response, string CorrelationId))state;
            response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        }, (context.Response, correlationId));

        // The correlation id rides alongside the trace id (see remarks), never in place of it:
        // tag the current Activity so both show up together wherever spans are inspected.
        Activity.Current?.AddTag("app.correlation_id", correlationId);

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var values) &&
            !StringValues.IsNullOrEmpty(values))
        {
            var incoming = values.ToString();
            if (!string.IsNullOrWhiteSpace(incoming))
            {
                return incoming;
            }
        }

        return Guid.NewGuid().ToString();
    }
}
