using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Web.CorrelationId;

namespace SbaCars.BuildingBlocks.Web.Auditing;

/// <summary>
/// Flushes every <see cref="ISensitiveDataAuditFlusher"/> registered for the request's scope
/// after the rest of the pipeline runs — on success and on exception alike — so a request that
/// only ever reads (no <c>SaveChanges</c>, ever) still ends up with a persisted sensitive-data
/// audit trail. This is the fix for the pendency §5.7 of the foundation plan left open in A4b:
/// opening a dossier to read CPF and income is exactly a read with nothing to flush it, unless
/// something guarantees a flush regardless of what the request handler did.
/// </summary>
/// <remarks>
/// <para>
/// <b>How it reaches the request's <c>DbContext</c> without knowing EF Core exists.</b>
/// <see cref="ISensitiveDataAuditFlusher"/> (<c>BuildingBlocks.Application</c>) is the only type
/// this middleware depends on. <c>BuildingBlocks.Persistence</c> registers one scoped
/// implementation per service's own <c>SbaCarsDbContext</c>
/// (<c>AddSbaCarsSensitiveDataAuditFlusher&lt;TContext&gt;</c>) that resolves, within the same DI
/// scope, to the very same context instance the request's repositories and queries used — the
/// <c>DbContext</c> is scoped per request, so "the flusher for this scope" and "the context this
/// request read from" are the same object. <c>BuildingBlocks.Web</c> never references EF Core or
/// <c>Microsoft.EntityFrameworkCore</c> to make this work. There can be zero, one, or (if a
/// service ever has more than one <c>DbContext</c>) several flushers registered; all of them are
/// flushed.
/// </para>
/// <para>
/// <b>Placement.</b> Must be registered after <c>UseExceptionHandler</c>, so it sits *inside* the
/// handler's try/catch: when a downstream request throws, this middleware's <c>finally</c> still
/// runs and flushes before the exception reaches the handler that turns it into a ProblemDetails
/// response — a request that fails still records what it read on the way to failing.
/// </para>
/// <para>
/// <b>Flush failure.</b> A failed flush is caught and logged here, never rethrown. Sensitive-data
/// auditing is a compliance concern layered on top of the request, not part of what the request
/// itself promises the caller — letting a broken audit write turn an otherwise successful
/// response into a 500 (or replace the real exception on a failed one) would make the audit
/// mechanism more disruptive than the silent gap it exists to close. It must not fail silently
/// either: every failure is logged at <see cref="LogLevel.Error"/> with the request's trace id
/// and correlation id, so a broken audit pipeline is an operable incident, not a quiet loss of
/// compliance data — someone has to be able to find it in the logs and act on it.
/// </para>
/// </remarks>
public sealed class SensitiveDataAuditFlushMiddleware(
    RequestDelegate next,
    ILogger<SensitiveDataAuditFlushMiddleware> logger)
{
    // IEnumerable<ISensitiveDataAuditFlusher> is scoped (BuildingBlocks.Persistence registers one
    // flusher per request-scoped DbContext) and therefore cannot be a constructor parameter here:
    // UseMiddleware<T> constructs a conventional middleware exactly once, from the application's
    // root service provider, which cannot resolve a scoped service. Per-request services belong on
    // InvokeAsync instead — the pipeline resolves those from HttpContext.RequestServices on every
    // call, which is exactly the per-request scope this needs.
    public async Task InvokeAsync(HttpContext context, IEnumerable<ISensitiveDataAuditFlusher> flushers)
    {
        try
        {
            await next(context);
        }
        finally
        {
            await FlushAllAsync(context, flushers);
        }
    }

    private async Task FlushAllAsync(HttpContext context, IEnumerable<ISensitiveDataAuditFlusher> flushers)
    {
        foreach (var flusher in flushers)
        {
            try
            {
                // CancellationToken.None, deliberately: by the time this runs the request is
                // already ending (successfully or not) and HttpContext.RequestAborted may already
                // be signaled. The whole point of this middleware is to still persist what was
                // read even though the request is over — racing the request's own cancellation
                // token would defeat that.
                await flusher.FlushAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                var correlationId = context.Items.TryGetValue(CorrelationIdMiddleware.HttpContextItemKey, out var value)
                    ? value as string
                    : null;

                logger.LogError(
                    ex,
                    "Failed to flush the sensitive-data audit trail for correlation {CorrelationId} " +
                    "(trace {TraceId}). The response already produced by this request was not affected, " +
                    "but any sensitive-data reads buffered for it may be lost — investigate as a compliance incident.",
                    correlationId,
                    Activity.Current?.Id);
            }
        }
    }
}
