using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using SbaCars.BuildingBlocks.Messaging.Inbox;
using SbaCars.BuildingBlocks.Persistence;

namespace SbaCars.BuildingBlocks.Messaging.Retention;

/// <summary>
/// Deletes expired rows from <c>{schema}.inbox_message</c> and sent rows from
/// <c>{schema}.outbox</c> — never unsent outbox rows (§6.3.2, B7).
/// </summary>
internal sealed class MessagingRetentionPurger(
    IOptions<PersistenceOptions> persistenceOptions,
    IOptions<MessagingOptions> messagingOptions,
    MessagingOutboxSchema schema,
    ILogger<MessagingRetentionPurger> logger)
{
    public async Task<PurgeResult> PurgeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var connectionString = persistenceOptions.Value.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Retention purge requires Persistence:ConnectionString to be configured.");
        }

        var retention = TimeSpan.FromDays(messagingOptions.Value.RetentionDays);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var inboxDeleted = await DeleteExpiredInboxRowsAsync(
            connection,
            schema.Value,
            retention,
            cancellationToken).ConfigureAwait(false);

        var outboxDeleted = await DeleteExpiredOutboxRowsAsync(
            connection,
            schema.Value,
            retention,
            cancellationToken).ConfigureAwait(false);

        if (inboxDeleted > 0 || outboxDeleted > 0)
        {
            logger.LogInformation(
                "Messaging retention purge completed for schema {Schema}: {InboxRowsDeleted} inbox rows, {OutboxRowsDeleted} sent outbox rows older than {RetentionDays} days.",
                schema.Value,
                inboxDeleted,
                outboxDeleted,
                messagingOptions.Value.RetentionDays);
        }

        MessagingMeters.PurgeRowsDeleted.Add(inboxDeleted, new KeyValuePair<string, object?>("table", "inbox"));
        MessagingMeters.PurgeRowsDeleted.Add(outboxDeleted, new KeyValuePair<string, object?>("table", "outbox"));

        return new PurgeResult(inboxDeleted, outboxDeleted);
    }

    private static async Task<long> DeleteExpiredInboxRowsAsync(
        NpgsqlConnection connection,
        string schema,
        TimeSpan retention,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
             DELETE FROM {schema}.inbox_message
             WHERE processed_at < now() - @retention;
             """;
        command.Parameters.AddWithValue("retention", retention);

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<long> DeleteExpiredOutboxRowsAsync(
        NpgsqlConnection connection,
        string schema,
        TimeSpan retention,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
             DELETE FROM {schema}.outbox
             WHERE "Sent" = TRUE
               AND "created_at" < now() - @retention;
             """;
        command.Parameters.AddWithValue("retention", retention);

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    internal sealed record PurgeResult(long InboxRowsDeleted, long OutboxRowsDeleted);
}
