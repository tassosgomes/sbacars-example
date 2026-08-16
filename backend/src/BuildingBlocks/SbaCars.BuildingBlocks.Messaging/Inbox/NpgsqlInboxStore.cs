using Npgsql;

namespace SbaCars.BuildingBlocks.Messaging.Inbox;

/// <summary>
/// Npgsql-backed <see cref="IInboxStore"/> against <c>{schema}.inbox_message</c> (§6.3, B3).
/// Uses the same <c>Persistence:ConnectionString</c> pool as the outbox forwarder — no extra AMQP
/// connections, no EF coupling.
/// </summary>
public sealed class NpgsqlInboxStore(string connectionString, string schema) : IInboxStore
{
    public async Task<bool> IsProcessedAsync(
        string messageId,
        string consumer,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
             SELECT EXISTS(
               SELECT 1
               FROM {schema}.inbox_message
               WHERE message_id = @message_id
                 AND consumer = @consumer);
             """;
        command.Parameters.AddWithValue("message_id", messageId);
        command.Parameters.AddWithValue("consumer", consumer);

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is true;
    }

    public async Task<bool> TryRecordProcessedAsync(
        string messageId,
        string consumer,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
             INSERT INTO {schema}.inbox_message (message_id, consumer, processed_at)
             VALUES (@message_id, @consumer, @processed_at);
             """;
        command.Parameters.AddWithValue("message_id", messageId);
        command.Parameters.AddWithValue("consumer", consumer);
        command.Parameters.AddWithValue("processed_at", DateTimeOffset.UtcNow);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return false;
        }
    }
}
