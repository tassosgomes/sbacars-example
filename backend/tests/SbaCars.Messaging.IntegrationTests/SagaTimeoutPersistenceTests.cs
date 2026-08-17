using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Sagas;
using SbaCars.BuildingBlocks.Messaging;
using SbaCars.BuildingBlocks.Messaging.Sagas;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Inventory.Infrastructure;
using SbaCars.TestKit;

namespace SbaCars.Messaging.IntegrationTests;

/// <summary>
/// B6 readiness (§2.5): saga data and deferred timeouts survive a process restart and the timeout
/// fires on the second host — proved against real RabbitMQ and PostgreSQL.
/// </summary>
[Collection(SbaCarsRabbitMqCollection.Name)]
public sealed class SagaTimeoutPersistenceTests : IAsyncLifetime, IClassFixture<SbaCarsPostgresFixture>
{
    private static readonly TimeSpan TimeoutDeferral = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan TimeoutWaitAfterRestart = TimeSpan.FromSeconds(20);

    private readonly SbaCarsRabbitMqFixture _rabbitMqFixture;
    private readonly SbaCarsPostgresFixture _postgresFixture;

    public SagaTimeoutPersistenceTests(
        SbaCarsRabbitMqFixture rabbitMqFixture,
        SbaCarsPostgresFixture postgresFixture)
    {
        _rabbitMqFixture = rabbitMqFixture;
        _postgresFixture = postgresFixture;
    }

    public async Task InitializeAsync()
    {
        var ownerConnectionString = _postgresFixture.ConnectionStringFor("own_inventory", "own_inventory_dev_pw");
        var inventoryOptions = new DbContextOptionsBuilder<InventoryDbContext>();
        inventoryOptions.UseSbaCarsNpgsql(ownerConnectionString, InventoryDbContext.Schema);
        await using var inventoryContext = new InventoryDbContext(inventoryOptions.Options);
        await inventoryContext.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DeferredTimeout_FiresAfterProcessRestart_WhenSagaPersistedInPostgreSql()
    {
        SagaTimeoutReceipt.Reset();

        var sagaId = Guid.NewGuid();
        var queueName = MessagingTestConfiguration.UniqueQueueName("saga-timeout");
        var configuration = BuildConfiguration(queueName);

        await using (var firstHost = await MessagingTestHost.StartAsync(services =>
                     ConfigureSagaHost(services, configuration)))
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));

            var bus = firstHost.Services.GetRequiredService<IBus>();
            await bus.SendLocal(new StartSagaTimeoutProbe(sagaId));

            var persisted = await WaitUntilSagaAndTimeoutPersistedAsync(
                sagaId,
                TimeSpan.FromSeconds(15));
            persisted.Should().BeTrue("the first host must persist saga data and schedule the deferred timeout");
        }

        var sagaCountAfterStop = await CountSagaRowsAsync();
        sagaCountAfterStop.Should().BeGreaterThan(0, "saga rows must survive disposing the first host");

        var timeoutCountAfterStop = await CountTimeoutRowsAsync();
        timeoutCountAfterStop.Should().BeGreaterThan(0, "timeout rows must survive disposing the first host");

        var dueTime = await GetEarliestTimeoutDueTimeAsync();
        dueTime.Should().NotBeNull();
        dueTime!.Value.Should().BeAfter(DateTimeOffset.UtcNow.AddSeconds(-1),
            "the deferred timeout must still be scheduled in PostgreSQL after the first host stops");

        await using var secondHost = await MessagingTestHost.StartAsync(services =>
            ConfigureSagaHost(services, configuration));

        var timeoutFired = await SagaTimeoutReceipt.WaitForAsync(sagaId, TimeoutWaitAfterRestart);
        timeoutFired.Should().BeTrue("the second host must dispatch the persisted timeout after restart");
    }

    [Fact]
    public async Task InventoryMigrator_CreatesSagaAndTimeoutTables_InInventorySchema()
    {
        await using var connection = new NpgsqlConnection(
            _postgresFixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"));
        await connection.OpenAsync();

        foreach (var tableName in new[] { "sagas", "saga_index", "timeouts" })
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = 'inventory'
                  AND table_name = @table_name;
                """;
            command.Parameters.AddWithValue("table_name", tableName);

            var exists = await command.ExecuteScalarAsync();
            exists.Should().NotBeNull($"inventory.{tableName} must exist after migrations are applied");
        }
    }

    private static void ConfigureSagaHost(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSbaCarsPersistenceOptions(configuration);
        services.AddSbaCarsMessaging(configuration, "saga-timeout-test", InventoryDbContext.Schema);
        services.AddRebusHandler<SagaTimeoutProbeSaga>();
    }

    private IConfiguration BuildConfiguration(string queueName)
    {
        return MessagingTestConfiguration.Build(
            _rabbitMqFixture,
            queueName,
            new Dictionary<string, string?>
            {
                ["Persistence:ConnectionString"] =
                    _postgresFixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"),
            });
    }

    private async Task<bool> WaitUntilSagaAndTimeoutPersistedAsync(Guid sagaId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (await CountSagaRowsAsync() > 0 && await CountTimeoutRowsAsync() > 0)
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        return false;
    }

    private async Task<long> CountSagaRowsAsync()
    {
        await using var connection = CreateSvcInventoryConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM inventory.sagas;";
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    private async Task<long> CountTimeoutRowsAsync()
    {
        await using var connection = CreateSvcInventoryConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM inventory.timeouts;";
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    private async Task<DateTimeOffset?> GetEarliestTimeoutDueTimeAsync()
    {
        await using var connection = CreateSvcInventoryConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT MIN(\"due_time\") FROM inventory.timeouts;";
        var result = await command.ExecuteScalarAsync();
        return result switch
        {
            null or DBNull => null,
            DateTimeOffset dateTimeOffset => dateTimeOffset,
            DateTime dateTime => new DateTimeOffset(dateTime, TimeSpan.Zero),
            _ => throw new InvalidOperationException($"Unexpected due_time type: {result.GetType().FullName}"),
        };
    }

    private NpgsqlConnection CreateSvcInventoryConnection()
    {
        return new NpgsqlConnection(
            _postgresFixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"));
    }

    public sealed record StartSagaTimeoutProbe(Guid SagaId);

    public sealed record SagaTimeoutFired(Guid SagaId);

    public sealed class SagaProbeData : ISagaData
    {
        public Guid Id { get; set; }

        public int Revision { get; set; }
    }

    public sealed class SagaTimeoutProbeSaga(
        ISagaTimeoutDeferral timeoutDeferral) : Saga<SagaProbeData>,
        IAmInitiatedBy<StartSagaTimeoutProbe>,
        IHandleMessages<SagaTimeoutFired>
    {
        protected override void CorrelateMessages(ICorrelationConfig<SagaProbeData> config)
        {
            config.Correlate<StartSagaTimeoutProbe>(message => message.SagaId, data => data.Id);
            config.Correlate<SagaTimeoutFired>(message => message.SagaId, data => data.Id);
        }

        public async Task Handle(StartSagaTimeoutProbe message)
        {
            Data.Id = message.SagaId;
            await timeoutDeferral
                .DeferLocalAsync(TimeoutDeferral, new SagaTimeoutFired(message.SagaId))
                .ConfigureAwait(false);
        }

        public Task Handle(SagaTimeoutFired message)
        {
            SagaTimeoutReceipt.Signal(message.SagaId);
            MarkAsComplete();
            return Task.CompletedTask;
        }
    }

    public static class SagaTimeoutReceipt
    {
        private static TaskCompletionSource<Guid> _signal =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public static void Reset()
        {
            _signal = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public static void Signal(Guid sagaId) => _signal.TrySetResult(sagaId);

        public static async Task<bool> WaitForAsync(Guid sagaId, TimeSpan timeout)
        {
            var completed = await Task.WhenAny(_signal.Task, Task.Delay(timeout)).ConfigureAwait(false);
            if (completed != _signal.Task)
            {
                return false;
            }

            var firedSagaId = await _signal.Task.ConfigureAwait(false);
            return firedSagaId == sagaId;
        }
    }
}
