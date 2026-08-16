using Microsoft.Extensions.Hosting;
using Rebus.Bus;
using SbaCars.Contracts.Foundation.V1;

namespace SbaCars.Catalog.Api.Messaging.Foundation;

/// <summary>
/// B5 scaffolding (§6.5): subscribes catalog to <c>foundation.ping</c> on startup. Delete with the
/// handler when the first real catalog consumer exists.
/// </summary>
public sealed class FoundationPingSubscriptionHostedService(IBus bus) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken) =>
        await bus.Subscribe<FoundationPingIntegrationEvent>();

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
