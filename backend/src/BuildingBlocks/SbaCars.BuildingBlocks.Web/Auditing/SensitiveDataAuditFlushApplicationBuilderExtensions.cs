using Microsoft.AspNetCore.Builder;

namespace SbaCars.BuildingBlocks.Web.Auditing;

/// <summary>
/// Wires <see cref="SensitiveDataAuditFlushMiddleware"/> into the pipeline.
/// </summary>
public static class SensitiveDataAuditFlushApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the end-of-request sensitive-data audit flush. Placed after <c>UseExceptionHandler</c>
    /// (so the flush still runs when the downstream pipeline throws) and, in practice, wherever a
    /// request may reach a <c>DbContext</c> — see <see cref="SensitiveDataAuditFlushMiddleware"/>
    /// for the full placement reasoning.
    /// </summary>
    public static IApplicationBuilder UseSbaCarsSensitiveDataAuditFlush(this IApplicationBuilder app) =>
        app.UseMiddleware<SensitiveDataAuditFlushMiddleware>();
}
