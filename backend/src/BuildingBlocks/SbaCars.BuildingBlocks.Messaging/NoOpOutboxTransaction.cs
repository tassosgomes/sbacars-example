using SbaCars.BuildingBlocks.Application;

namespace SbaCars.BuildingBlocks.Messaging;

/// <summary>
/// No-op outbox session used when a host registers messaging without persistence (B1 integration
/// tests) or before a service's Infrastructure replaces this registration with
/// <see cref="SbaCars.BuildingBlocks.Persistence.EfUnitOfWork{TContext}"/>.
/// </summary>
internal sealed class NoOpOutboxTransaction : IOutboxTransaction
{
    public Task EnsureOpenAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
