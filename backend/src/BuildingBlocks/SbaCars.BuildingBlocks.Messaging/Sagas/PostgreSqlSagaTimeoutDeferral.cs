using Microsoft.Extensions.Options;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.PostgreSql;
using Rebus.PostgreSql.Timeouts;
using Rebus.Serialization;
using Rebus.Serialization.Json;
using Rebus.Time;

namespace SbaCars.BuildingBlocks.Messaging.Sagas;

/// <summary>
/// Writes deferred saga timeouts directly through <see cref="PostgreSqlTimeoutManager"/> so
/// scheduling does not pass through the outbox-decorated transport (see
/// <see cref="ISagaTimeoutDeferral"/>). The bus' own timeout manager continues to poll the same
/// table.
/// </summary>
internal sealed class PostgreSqlSagaTimeoutDeferral : ISagaTimeoutDeferral
{
    private readonly PostgreSqlTimeoutManager _timeoutManager;
    private readonly ISerializer _serializer = new SystemTextJsonRebusSerializer();
    private readonly IRebusTime _rebusTime = new DefaultRebusTime();
    private readonly string _inputQueueName;

    public PostgreSqlSagaTimeoutDeferral(
        IPostgresConnectionProvider connectionProvider,
        string schemaName,
        IOptions<MessagingOptions> messagingOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        _inputQueueName = messagingOptions.Value.InputQueueName;
        _timeoutManager = new PostgreSqlTimeoutManager(
            connectionProvider,
            "timeouts",
            new NullLoggerFactory(),
            _rebusTime,
            schemaName);
    }

    public async Task DeferLocalAsync(
        TimeSpan delay,
        object message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var outgoing = new Message(
            new Dictionary<string, string>
            {
                [Headers.DeferredRecipient] = _inputQueueName,
            },
            message);
        var transportMessage = await _serializer.Serialize(outgoing).ConfigureAwait(false);

        await _timeoutManager.Defer(
            _rebusTime.Now.Add(delay),
            transportMessage.Headers,
            transportMessage.Body).ConfigureAwait(false);
    }
}
