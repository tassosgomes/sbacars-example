using SbaCars.BuildingBlocks.Application;

namespace SbaCars.BuildingBlocks.Persistence.Auditing;

/// <summary>
/// The <see cref="ISensitiveDataAuditFlusher"/> implementation registered for every service:
/// delegates straight to <see cref="SbaCarsDbContext.FlushSensitiveDataAuditAsync"/> on the
/// request-scoped <typeparamref name="TContext"/>. See
/// <see cref="SensitiveDataAuditFlusherServiceCollectionExtensions.AddSbaCarsSensitiveDataAuditFlusher{TContext}"/>
/// for how it is wired up.
/// </summary>
internal sealed class SensitiveDataAuditFlusher<TContext>(TContext context) : ISensitiveDataAuditFlusher
    where TContext : SbaCarsDbContext
{
    public Task FlushAsync(CancellationToken cancellationToken = default) =>
        context.FlushSensitiveDataAuditAsync(cancellationToken);
}
