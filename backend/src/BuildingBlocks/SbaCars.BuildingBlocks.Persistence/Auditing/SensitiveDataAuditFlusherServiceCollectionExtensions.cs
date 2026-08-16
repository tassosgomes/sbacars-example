using Microsoft.Extensions.DependencyInjection;
using SbaCars.BuildingBlocks.Application;

namespace SbaCars.BuildingBlocks.Persistence.Auditing;

public static class SensitiveDataAuditFlusherServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="ISensitiveDataAuditFlusher"/> that
    /// <c>SensitiveDataAuditFlushMiddleware</c> (<c>BuildingBlocks.Web</c>) calls at the end of
    /// every request to persist whatever sensitive-data reads <typeparamref name="TContext"/>
    /// buffered (§5.7 of the foundation plan).
    /// </summary>
    /// <remarks>
    /// Safe — and meant — to call for every service regardless of whether it has wired
    /// <see cref="SensitiveDataAuditInterceptor"/> into <typeparamref name="TContext"/> yet: until
    /// a service introduces its first <c>ISensitiveDataEntity</c>,
    /// <see cref="SbaCarsDbContext.FlushSensitiveDataAuditAsync"/> is a no-op (no interceptor was
    /// supplied to the context), so the flush this triggers costs nothing. Calling this once, from
    /// the same <c>Add&lt;Service&gt;Infrastructure</c> that registers <typeparamref name="TContext"/>
    /// itself, is what makes the end-of-request flush present from day one — never something a
    /// future service has to remember to add on the day it marks its first entity sensitive.
    /// </remarks>
    public static IServiceCollection AddSbaCarsSensitiveDataAuditFlusher<TContext>(this IServiceCollection services)
        where TContext : SbaCarsDbContext
    {
        services.AddScoped<ISensitiveDataAuditFlusher, SensitiveDataAuditFlusher<TContext>>();
        return services;
    }
}
