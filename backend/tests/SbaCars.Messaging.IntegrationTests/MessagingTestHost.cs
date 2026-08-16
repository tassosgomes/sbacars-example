using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SbaCars.Messaging.IntegrationTests;

/// <summary>
/// Boots a real Rebus bus (via <c>AddSbaCarsMessaging</c>) on a plain <see cref="ServiceProvider"/> —
/// no ASP.NET Core host is needed here, since bootstrapping <c>AddSbaCarsMessaging</c> only requires
/// <c>Microsoft.Extensions.DependencyInjection</c>/<c>Rebus.ServiceProvider</c>, both of which flow
/// transitively from <c>SbaCars.BuildingBlocks.Messaging</c>.
/// </summary>
/// <remarks>
/// Deliberately starts every registered <see cref="IHostedService"/> by calling
/// <see cref="IHostedService.StartAsync"/> directly instead of the more obvious
/// <c>Rebus.Config.ServiceProviderExtensions.StartHostedServices(IServiceProvider)</c> helper: that
/// helper blocks the calling thread synchronously on the bus' async startup path, and with
/// <c>RabbitMQ.Client</c> 7.x's connection being fully async under the hood, that synchronous wait
/// never returns — confirmed empirically while writing this test suite (a hand-rolled harness using
/// <c>StartHostedServices</c> hung indefinitely at connection time; the same harness using this
/// class' async start/stop loop worked immediately). This is a trap for a hand-rolled host like this
/// one, not a defect in <c>AddSbaCarsMessaging</c> itself: every real host these tests stand in
/// for — the four services' own ASP.NET Core <c>WebApplication</c> — already starts hosted services
/// asynchronously (<c>await app.StartAsync()</c>), so production is unaffected.
/// </remarks>
internal sealed class MessagingTestHost : IAsyncDisposable
{
    private readonly ServiceProvider _provider;

    private MessagingTestHost(ServiceProvider provider)
    {
        _provider = provider;
    }

    public IServiceProvider Services => _provider;

    public static async Task<MessagingTestHost> StartAsync(
        Action<IServiceCollection> configureServices, CancellationToken cancellationToken = default)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug());
        configureServices(services);

        var provider = services.BuildServiceProvider();

        foreach (var hostedService in provider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(cancellationToken);
        }

        return new MessagingTestHost(provider);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var hostedService in _provider.GetServices<IHostedService>())
        {
            try
            {
                await hostedService.StopAsync(CancellationToken.None);
            }
            catch
            {
                // Best-effort shutdown: a test that already proved its point should not fail on
                // teardown because a connection the test itself severed (e.g.
                // RabbitMqReadinessHealthCheckTests' dead-port scenario) is no longer there to stop
                // cleanly.
            }
        }

        await _provider.DisposeAsync();
    }
}
