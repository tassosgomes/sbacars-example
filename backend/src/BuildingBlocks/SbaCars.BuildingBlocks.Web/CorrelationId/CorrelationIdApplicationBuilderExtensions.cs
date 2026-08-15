using Microsoft.AspNetCore.Builder;

namespace SbaCars.BuildingBlocks.Web.CorrelationId;

/// <summary>
/// Wires <see cref="CorrelationIdMiddleware"/> into the pipeline.
/// </summary>
public static class CorrelationIdApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the correlation id middleware. Placed after <c>UseExceptionHandler</c> (so the
    /// handler can still catch anything the middleware itself might do) and before anything
    /// that logs, so every downstream log line can carry the correlation id.
    /// </summary>
    public static IApplicationBuilder UseSbaCarsCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();
}
