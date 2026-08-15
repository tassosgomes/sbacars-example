using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SbaCars.BuildingBlocks.Web.CorrelationId;

namespace SbaCars.BuildingBlocks.Web.ErrorHandling;

/// <summary>
/// Last line of defense for every unhandled exception in the pipeline. Turns any exception into
/// an RFC 9457 <see cref="ProblemDetails"/> response and logs the failure server-side with the
/// same <c>traceId</c> the client receives, so support can correlate a ticket to a log line
/// without the response ever carrying a stack trace, an exception message it did not vet, or an
/// internal type name.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ExceptionProblemDetailsMap exceptionMap,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private const string GenericDetail =
        "An unexpected error occurred. Contact support with the trace id below.";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var isMapped = exceptionMap.TryResolve(exception, out var statusCode, out var title);
        if (!isMapped)
        {
            statusCode = StatusCodes.Status500InternalServerError;
            title = "An unexpected error occurred.";
        }

        var traceId = ResolveTraceId(httpContext);
        var correlationId = ResolveCorrelationId(httpContext);

        // Unmapped exceptions are logged at their full fidelity (message, stack trace) on the
        // server only. What crosses the wire to the client is always the sanitized body below.
        logger.LogError(
            exception,
            "Request {Method} {Path} failed with status {StatusCode}. TraceId={TraceId} CorrelationId={CorrelationId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            statusCode,
            traceId,
            correlationId);

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = isMapped ? exception.Message : GenericDetail,
            Type = $"https://httpstatuses.io/{statusCode}",
            Instance = httpContext.Request.Path,
        };
        problemDetails.Extensions["traceId"] = traceId;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails,
        });
    }

    /// <summary>
    /// The W3C trace id of the current <see cref="Activity"/> when tracing is active, falling
    /// back to ASP.NET Core's own per-request identifier otherwise. Either way, the same value
    /// returned to the client is the one written to the log line above.
    /// </summary>
    private static string ResolveTraceId(HttpContext httpContext) =>
        Activity.Current?.TraceId.ToHexString() ?? httpContext.TraceIdentifier;

    private static string? ResolveCorrelationId(HttpContext httpContext) =>
        httpContext.Items.TryGetValue(CorrelationIdMiddleware.HttpContextItemKey, out var value)
            ? value as string
            : null;
}
