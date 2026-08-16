using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Messages;
using SbaCars.BuildingBlocks.Messaging;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Contracts;
using SbaCars.Inventory.Infrastructure;
using SbaCars.TestKit;

namespace SbaCars.Messaging.IntegrationTests;

/// <summary>
/// B3 readiness (§6.3): re-delivery of the same <c>rbs2-msg-id</c> does not duplicate handler
/// effects — proved against real RabbitMQ and PostgreSQL.
/// </summary>
[Collection(SbaCarsRabbitMqCollection.Name)]
public sealed class InboxIdempotencyTests : IAsyncLifetime, IClassFixture<SbaCarsPostgresFixture>
{
    private const string ServiceName = "inbox-idempotency-test";

    private readonly SbaCarsRabbitMqFixture _rabbitMqFixture;
    private readonly SbaCarsPostgresFixture _postgresFixture;

    public InboxIdempotencyTests(
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
    public async Task SameMessageId_IsHandledOnce_OnRedelivery_InboxHasOneRow()
    {
        InboxProbeEventHandler.Reset();

        var queueName = MessagingTestConfiguration.UniqueQueueName("inbox-dedup");
        var configuration = BuildConfiguration(queueName);
        var messageId = Guid.NewGuid().ToString();

        await using var host = await MessagingTestHost.StartAsync(services =>
            ConfigureInboxHost(services, configuration));

        var bus = host.Services.GetRequiredService<IBus>();
        await bus.Subscribe<InboxProbeEvent>();
        await Task.Delay(TimeSpan.FromMilliseconds(300));

        var headers = new Dictionary<string, string> { [Headers.MessageId] = messageId };
        await bus.Publish(new InboxProbeEvent("first-delivery"), headers);
        var firstHandled = await InboxProbeEventHandler.WaitForHandleCountAsync(1, TimeSpan.FromSeconds(15));
        firstHandled.Should().BeTrue("the first delivery must reach the handler");

        await bus.Publish(new InboxProbeEvent("redelivery"), headers);
        await Task.Delay(TimeSpan.FromSeconds(2));

        InboxProbeEventHandler.HandleCount.Should().Be(1, "redelivery of the same rbs2-msg-id must not run the handler again");

        var inboxCount = await CountInboxRowsAsync(messageId, ServiceName);
        inboxCount.Should().Be(1, "exactly one (message_id, consumer) row must exist after a successful handle");
    }

    [Fact]
    public async Task DifferentMessageIds_AreBothProcessed()
    {
        InboxProbeEventHandler.Reset();

        var queueName = MessagingTestConfiguration.UniqueQueueName("inbox-distinct");
        var configuration = BuildConfiguration(queueName);

        await using var host = await MessagingTestHost.StartAsync(services =>
            ConfigureInboxHost(services, configuration));

        var bus = host.Services.GetRequiredService<IBus>();
        await bus.Subscribe<InboxProbeEvent>();
        await Task.Delay(TimeSpan.FromMilliseconds(300));

        var firstId = Guid.NewGuid().ToString();
        var secondId = Guid.NewGuid().ToString();

        await bus.Publish(
            new InboxProbeEvent("first"),
            new Dictionary<string, string> { [Headers.MessageId] = firstId });
        await bus.Publish(
            new InboxProbeEvent("second"),
            new Dictionary<string, string> { [Headers.MessageId] = secondId });

        var bothHandled = await InboxProbeEventHandler.WaitForHandleCountAsync(2, TimeSpan.FromSeconds(15));
        bothHandled.Should().BeTrue("distinct message ids must each be processed once");

        (await CountInboxRowsAsync(firstId, ServiceName)).Should().Be(1);
        (await CountInboxRowsAsync(secondId, ServiceName)).Should().Be(1);
    }

    [Fact]
    public async Task HandlerFailure_DoesNotRecordInbox_LaterSuccessIsProcessedOnce()
    {
        FlakyInboxProbeEventHandler.Reset();

        var queueName = MessagingTestConfiguration.UniqueQueueName("inbox-retry");
        var configuration = BuildConfiguration(queueName, new Dictionary<string, string?>
        {
            ["Messaging:MaxDeliveryAttempts"] = "3",
        });
        var messageId = Guid.NewGuid().ToString();

        await using var host = await MessagingTestHost.StartAsync(services =>
        {
            services.AddSbaCarsPersistenceOptions(configuration);
            services.AddSbaCarsMessaging(configuration, ServiceName, InventoryDbContext.Schema);
            services.AddRebusHandler<FlakyInboxProbeEventHandler>();
        });

        var bus = host.Services.GetRequiredService<IBus>();
        await bus.Subscribe<InboxProbeEvent>();
        await Task.Delay(TimeSpan.FromMilliseconds(300));

        await bus.Publish(
            new InboxProbeEvent("flaky"),
            new Dictionary<string, string> { [Headers.MessageId] = messageId });

        var succeeded = await FlakyInboxProbeEventHandler.WaitForSuccessCountAsync(1, TimeSpan.FromSeconds(15));
        succeeded.Should().BeTrue("Rebus must retry until the handler succeeds");

        FlakyInboxProbeEventHandler.AttemptCount.Should().BeGreaterThan(1);

        var inboxCountAfterSuccess = await CountInboxRowsAsync(messageId, ServiceName);
        inboxCountAfterSuccess.Should().Be(1, "inbox row is recorded only after a successful handle");

        await bus.Publish(
            new InboxProbeEvent("redelivery-after-success"),
            new Dictionary<string, string> { [Headers.MessageId] = messageId });
        await Task.Delay(TimeSpan.FromSeconds(2));

        FlakyInboxProbeEventHandler.SuccessCount.Should().Be(1, "redelivery after success must be deduplicated");
    }

    private static void ConfigureInboxHost(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSbaCarsPersistenceOptions(configuration);
        services.AddSbaCarsMessaging(configuration, ServiceName, InventoryDbContext.Schema);
        services.AddRebusHandler<InboxProbeEventHandler>();
    }

    private IConfiguration BuildConfiguration(string queueName, IDictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Persistence:ConnectionString"] =
                _postgresFixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"),
        };

        if (overrides is not null)
        {
            foreach (var (key, value) in overrides)
            {
                values[key] = value;
            }
        }

        return MessagingTestConfiguration.Build(_rabbitMqFixture, queueName, values);
    }

    private async Task<long> CountInboxRowsAsync(string messageId, string consumer)
    {
        await using var connection = new NpgsqlConnection(
            _postgresFixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(*)
            FROM inventory.inbox_message
            WHERE message_id = @message_id
              AND consumer = @consumer;
            """;
        command.Parameters.AddWithValue("message_id", messageId);
        command.Parameters.AddWithValue("consumer", consumer);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    [IntegrationEvent("test.messaging-inbox-probe")]
    public sealed record InboxProbeEvent(string ProbeName);

    public sealed class InboxProbeEventHandler : IHandleMessages<InboxProbeEvent>
    {
        private static int _handleCount;

        public static int HandleCount => _handleCount;

        public static void Reset()
        {
            Interlocked.Exchange(ref _handleCount, 0);
        }

        public Task Handle(InboxProbeEvent message)
        {
            Interlocked.Increment(ref _handleCount);
            return Task.CompletedTask;
        }

        public static async Task<bool> WaitForHandleCountAsync(int expected, TimeSpan timeout)
        {
            var deadline = DateTimeOffset.UtcNow.Add(timeout);
            while (DateTimeOffset.UtcNow < deadline)
            {
                if (Volatile.Read(ref _handleCount) >= expected)
                {
                    return true;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            return false;
        }
    }

    public sealed class FlakyInboxProbeEventHandler : IHandleMessages<InboxProbeEvent>
    {
        private static int _attemptCount;
        private static int _successCount;

        public static int AttemptCount => _attemptCount;

        public static int SuccessCount => _successCount;

        public static void Reset()
        {
            Interlocked.Exchange(ref _attemptCount, 0);
            Interlocked.Exchange(ref _successCount, 0);
        }

        public Task Handle(InboxProbeEvent message)
        {
            var attempt = Interlocked.Increment(ref _attemptCount);
            if (attempt < 2)
            {
                throw new InvalidOperationException("not ready yet");
            }

            Interlocked.Increment(ref _successCount);
            return Task.CompletedTask;
        }

        public static async Task<bool> WaitForSuccessCountAsync(int expected, TimeSpan timeout)
        {
            var deadline = DateTimeOffset.UtcNow.Add(timeout);
            while (DateTimeOffset.UtcNow < deadline)
            {
                if (Volatile.Read(ref _successCount) >= expected)
                {
                    return true;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            return false;
        }
    }
}
