using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace SbaCars.BuildingBlocks.Messaging.HealthChecks;

/// <summary>
/// Owns the single long-lived RabbitMQ connection <see cref="RabbitMqReadinessHealthCheck"/> watches
/// (D10, §6.3.1) — registered as a singleton by <c>MessagingHealthChecksExtensions.AddSbaCarsRabbitMqReadinessCheck</c>,
/// opened lazily on the first probe, and kept open for the life of the process.
/// </summary>
/// <remarks>
/// <para>
/// <b>Connection budget, confirmed rather than merely planned (§6.3.1).</b> §6.3.1 sizes the whole
/// topology against a managed broker's connection ceiling (the CloudAMQP free/shared plan tops out
/// at 40). Each of the four service processes now opens exactly two AMQP connections: one owned by
/// Rebus itself — its transport's <c>ConnectionManager</c> holds a single <c>_activeConnection</c>
/// for the whole bus, per <c>MessagingTopology</c>'s remarks on why the topology is one bus per
/// service, not one per event type — and this one, opened solely by the readiness probe below. That
/// is 1 (Rebus) + 1 (probe) = 2 connections per process, which is exactly what §6.3.1 budgeted; this
/// class is what keeps that number true at runtime instead of just on paper. A health check that
/// opened a fresh connection per poll (or per replica of the readiness endpoint being scraped) would
/// make that number depend on polling frequency instead of staying fixed at 2 — which is precisely
/// what this singleton, reused across every <see cref="IsOpenAsync"/> call, prevents.
/// </para>
/// <para>
/// The probe's own connection is deliberately configured independently of Rebus' (own
/// <see cref="ConnectionFactory"/>, own <c>ClientProvidedName</c>) so that a broker-side view of
/// connections (e.g. the management UI) can tell the two apart, and so that this class carries no
/// dependency on how Rebus happens to be wired.
/// </para>
/// </remarks>
public sealed class RabbitMqConnectionProbe(IOptions<MessagingOptions> options) : IAsyncDisposable
{
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;

    /// <summary>
    /// Returns whether the probe's connection is currently open, opening it first if this is the
    /// first call or the previous connection was lost. Lets exceptions from
    /// <see cref="IConnectionFactory.CreateConnectionAsync(System.Threading.CancellationToken)"/>
    /// propagate to the caller — <see cref="RabbitMqReadinessHealthCheck"/> is what turns those into
    /// an <c>Unhealthy</c> result with the exception attached, mirroring
    /// <c>EfCoreReadinessHealthCheck{TContext}</c>'s try/catch shape.
    /// </summary>
    public async Task<bool> IsOpenAsync(CancellationToken cancellationToken)
    {
        var current = _connection;
        if (current is { IsOpen: true })
        {
            return true;
        }

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return true;
            }

            if (_connection is not null)
            {
                // The previous connection closed on its own (broker restart, network blip). There is
                // nothing to recover from it — just let it go before opening a fresh one.
                try
                {
                    await _connection.DisposeAsync();
                }
                catch
                {
                    // Best-effort cleanup of an already-dead connection; the failure here carries no
                    // information the caller needs, and the connection is being replaced regardless.
                }
            }

            var settings = options.Value;
            var factory = new ConnectionFactory
            {
                Uri = new Uri(settings.ConnectionString),
                AutomaticRecoveryEnabled = true,
                RequestedHeartbeat = TimeSpan.FromSeconds(settings.HeartbeatSeconds),
                ClientProvidedName = $"{settings.InputQueueName}/health-probe",
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            return _connection.IsOpen;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is null)
        {
            return;
        }

        try
        {
            await _connection.CloseAsync();
        }
        catch
        {
            // Same "closing can throw" caveat Rebus.RabbitMq's own ConnectionManager guards
            // against — irrelevant during shutdown.
        }

        await _connection.DisposeAsync();
    }
}
