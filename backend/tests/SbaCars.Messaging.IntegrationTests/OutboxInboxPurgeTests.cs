using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SbaCars.BuildingBlocks.Messaging;
using SbaCars.BuildingBlocks.Messaging.Inbox;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Inventory.Infrastructure;
using SbaCars.TestKit;

namespace SbaCars.Messaging.IntegrationTests;

/// <summary>
/// B7 readiness (§6.3.2): outbox/inbox retention purge runs on exactly one replica per schema,
/// proved against real PostgreSQL with session-level advisory locks.
/// </summary>
[Collection(SbaCarsRabbitMqCollection.Name)]
public sealed class OutboxInboxPurgeTests : IAsyncLifetime, IClassFixture<SbaCarsPostgresFixture>
{
    private const string OldInboxMessageId = "purge-test-old-inbox";
    private const string FreshInboxMessageId = "purge-test-fresh-inbox";
    private const string OldSentOutboxDestination = "purge-test-old-sent-outbox";
    private const string UnsentOutboxDestination = "purge-test-unsent-outbox";
    private const string RecentSentOutboxDestination = "purge-test-recent-sent-outbox";

    private readonly SbaCarsRabbitMqFixture _rabbitMqFixture;
    private readonly SbaCarsPostgresFixture _postgresFixture;

    public OutboxInboxPurgeTests(
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
    public async Task TwoReplicas_ExactlyOnePurgeCycle_DeletesOnlyExpiredRows()
    {
        await SeedRetentionProbeRowsAsync();

        using var meterListener = CreatePurgeCycleMeterListener(out var purgeCycleCount);

        var queueNameReplicaA = MessagingTestConfiguration.UniqueQueueName("purge-replica-a");
        var queueNameReplicaB = MessagingTestConfiguration.UniqueQueueName("purge-replica-b");
        var configurationReplicaA = BuildConfiguration(queueNameReplicaA);
        var configurationReplicaB = BuildConfiguration(queueNameReplicaB);

        await using var replicaA = await MessagingTestHost.StartAsync(services =>
            ConfigurePurgeHost(services, configurationReplicaA, "purge-replica-a"));
        await using var replicaB = await MessagingTestHost.StartAsync(services =>
            ConfigurePurgeHost(services, configurationReplicaB, "purge-replica-b"));

        var purged = await WaitUntilExpiredRowsPurgedAsync(TimeSpan.FromSeconds(30));
        purged.Should().BeTrue("the lock leader must purge expired rows on startup");

        purgeCycleCount().Should().Be(1, "exactly one replica must complete a purge cycle");

        (await InboxRowExistsAsync(OldInboxMessageId)).Should().BeFalse("expired inbox rows must be deleted");
        (await InboxRowExistsAsync(FreshInboxMessageId)).Should().BeTrue("fresh inbox rows must survive purge");

        (await OutboxRowExistsAsync(OldSentOutboxDestination)).Should().BeFalse("expired sent outbox rows must be deleted");
        (await OutboxRowExistsAsync(UnsentOutboxDestination)).Should().BeTrue("unsent outbox rows must never be purged");
        (await OutboxRowExistsAsync(RecentSentOutboxDestination)).Should().BeTrue("recent sent outbox rows must survive purge");
    }

    private static void ConfigurePurgeHost(IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        services.AddSbaCarsPersistenceOptions(configuration);
        services.AddSbaCarsMessaging(configuration, serviceName, InventoryDbContext.Schema);
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
                ["Messaging:PurgeInterval"] = "01:00:00",
            });
    }

    private async Task SeedRetentionProbeRowsAsync()
    {
        var expiredAt = DateTimeOffset.UtcNow.AddDays(-8);
        var freshAt = DateTimeOffset.UtcNow;

        await using var connection = new NpgsqlConnection(
            _postgresFixture.ConnectionStringFor("own_inventory", "own_inventory_dev_pw"));
        await connection.OpenAsync();

        await using var cleanup = connection.CreateCommand();
        cleanup.CommandText =
            """
            DELETE FROM inventory.inbox_message
            WHERE message_id LIKE 'purge-test-%';

            DELETE FROM inventory.outbox
            WHERE "DestinationAddress" LIKE 'purge-test-%';
            """;
        await cleanup.ExecuteNonQueryAsync();

        await using var seed = connection.CreateCommand();
        seed.CommandText =
            """
            INSERT INTO inventory.inbox_message (message_id, consumer, processed_at)
            VALUES
              (@old_inbox_message_id, 'purge-seed', @expired_at),
              (@fresh_inbox_message_id, 'purge-seed', @fresh_at);

            INSERT INTO inventory.outbox ("DestinationAddress", "Sent", "created_at")
            VALUES
              (@old_sent_destination, TRUE, @expired_at),
              (@unsent_destination, FALSE, @expired_at),
              (@recent_sent_destination, TRUE, @fresh_at);
            """;
        seed.Parameters.AddWithValue("old_inbox_message_id", OldInboxMessageId);
        seed.Parameters.AddWithValue("fresh_inbox_message_id", FreshInboxMessageId);
        seed.Parameters.AddWithValue("old_sent_destination", OldSentOutboxDestination);
        seed.Parameters.AddWithValue("unsent_destination", UnsentOutboxDestination);
        seed.Parameters.AddWithValue("recent_sent_destination", RecentSentOutboxDestination);
        seed.Parameters.AddWithValue("expired_at", expiredAt);
        seed.Parameters.AddWithValue("fresh_at", freshAt);
        await seed.ExecuteNonQueryAsync();
    }

    private async Task<bool> WaitUntilExpiredRowsPurgedAsync(TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (!await InboxRowExistsAsync(OldInboxMessageId) &&
                !await OutboxRowExistsAsync(OldSentOutboxDestination))
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        return false;
    }

    private async Task<bool> InboxRowExistsAsync(string messageId)
    {
        await using var connection = CreateSvcInventoryConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT EXISTS(
              SELECT 1
              FROM inventory.inbox_message
              WHERE message_id = @message_id);
            """;
        command.Parameters.AddWithValue("message_id", messageId);
        var result = await command.ExecuteScalarAsync();
        return result is true;
    }

    private async Task<bool> OutboxRowExistsAsync(string destinationAddress)
    {
        await using var connection = CreateSvcInventoryConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT EXISTS(
              SELECT 1
              FROM inventory.outbox
              WHERE "DestinationAddress" = @destination_address);
            """;
        command.Parameters.AddWithValue("destination_address", destinationAddress);
        var result = await command.ExecuteScalarAsync();
        return result is true;
    }

    private NpgsqlConnection CreateSvcInventoryConnection()
    {
        return new NpgsqlConnection(
            _postgresFixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"));
    }

    private static MeterListener CreatePurgeCycleMeterListener(out Func<long> readCount)
    {
        long purgeCycleCount = 0;

        var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == MessagingMeters.Name &&
                    instrument.Name == "messaging.purge.cycles")
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            },
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
        {
            if (instrument.Name == "messaging.purge.cycles")
            {
                Interlocked.Add(ref purgeCycleCount, measurement);
            }
        });

        listener.Start();
        readCount = () => Interlocked.Read(ref purgeCycleCount);
        return listener;
    }
}
