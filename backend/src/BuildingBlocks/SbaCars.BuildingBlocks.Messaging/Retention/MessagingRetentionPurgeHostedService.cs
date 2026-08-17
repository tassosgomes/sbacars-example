using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using SbaCars.BuildingBlocks.Messaging.Inbox;
using SbaCars.BuildingBlocks.Persistence;

namespace SbaCars.BuildingBlocks.Messaging.Retention;

/// <summary>
/// Session-level PostgreSQL advisory lock leader election (§6.3.2, B7): one replica per schema
/// holds <c>pg_try_advisory_lock</c> on a dedicated connection and runs retention purge on acquire
/// and on <see cref="MessagingOptions.PurgeInterval"/>.
/// </summary>
internal sealed class MessagingRetentionPurgeHostedService(
    IOptions<PersistenceOptions> persistenceOptions,
    IOptions<MessagingOptions> messagingOptions,
    MessagingOutboxSchema schema,
    MessagingRetentionPurger purger,
    ILogger<MessagingRetentionPurgeHostedService> logger) : BackgroundService
{
    private const string AdvisoryLockNamespace = "sbacars.messaging.purge";
    private static readonly TimeSpan LockRetryDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = persistenceOptions.Value.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Retention purge hosted service requires Persistence:ConnectionString to be configured.");
        }

        var purgeInterval = messagingOptions.Value.PurgeInterval;
        if (purgeInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                $"{MessagingOptions.SectionName}:{nameof(MessagingOptions.PurgeInterval)} must be greater than zero.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var lockConnection = new NpgsqlConnection(connectionString);
            await lockConnection.OpenAsync(stoppingToken).ConfigureAwait(false);

            var acquired = await TryAcquireAdvisoryLockAsync(lockConnection, schema.Value, stoppingToken)
                .ConfigureAwait(false);

            if (!acquired)
            {
                logger.LogDebug(
                    "Messaging retention purge lock not acquired for schema {Schema}; retrying in {RetryDelay}.",
                    schema.Value,
                    LockRetryDelay);

                try
                {
                    await Task.Delay(LockRetryDelay, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                continue;
            }

            logger.LogInformation(
                "Messaging retention purge leader elected for schema {Schema}.",
                schema.Value);

            try
            {
                await RunPurgeCycleAsync(stoppingToken).ConfigureAwait(false);

                using var timer = new PeriodicTimer(purgeInterval);
                while (!stoppingToken.IsCancellationRequested)
                {
                    var ticked = await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
                    if (!ticked)
                    {
                        break;
                    }

                    await RunPurgeCycleAsync(stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutdown while leader — lock is released when the connection disposes.
            }
        }
    }

    private async Task RunPurgeCycleAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await purger.PurgeAsync(cancellationToken).ConfigureAwait(false);
            MessagingMeters.PurgeCycles.Add(1);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Messaging retention purge cycle failed for schema {Schema}; the leader will retry on the next interval.",
                schema.Value);
        }
    }

    private static async Task<bool> TryAcquireAdvisoryLockAsync(
        NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT pg_try_advisory_lock(
              hashtext(@lock_namespace),
              hashtext(@schema));
            """;
        command.Parameters.AddWithValue("lock_namespace", AdvisoryLockNamespace);
        command.Parameters.AddWithValue("schema", schema);

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is true;
    }
}
