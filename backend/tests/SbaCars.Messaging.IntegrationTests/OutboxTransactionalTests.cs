using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Messaging;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Contracts;
using SbaCars.Inventory.Infrastructure;
using SbaCars.TestKit;

namespace SbaCars.Messaging.IntegrationTests;

/// <summary>
/// B2 readiness (§12): integration events enlisted in the PostgreSQL outbox commit with the EF
/// transaction and reach the broker only after commit — rollback leaves neither probe data, nor
/// outbox rows, nor a handled message.
/// </summary>
[Collection(SbaCarsRabbitMqCollection.Name)]
public sealed class OutboxTransactionalTests : IAsyncLifetime, IClassFixture<SbaCarsPostgresFixture>
{
    private readonly SbaCarsRabbitMqFixture _rabbitMqFixture;
    private readonly SbaCarsPostgresFixture _postgresFixture;

    public OutboxTransactionalTests(
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
        await using (var inventoryContext = new InventoryDbContext(inventoryOptions.Options))
        {
            await inventoryContext.Database.MigrateAsync();
        }

        await CreateOutboxProbeTableAsync(ownerConnectionString);
    }

    private static async Task CreateOutboxProbeTableAsync(string ownerConnectionString)
    {
        await using var connection = new NpgsqlConnection(ownerConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS inventory.outbox_probe
            (
              id uuid NOT NULL PRIMARY KEY,
              name character varying(200) NOT NULL,
              CONSTRAINT outbox_probe_reject_forced_rollback CHECK (name NOT LIKE 'force-rollback%')
            );
            """;
        await command.ExecuteNonQueryAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Commit_PersistsProbeRow_ForwardsOutboxMessage_AndHandlerReceivesEvent()
    {
        OutboxProbeEventHandler.Reset();

        var queueName = MessagingTestConfiguration.UniqueQueueName("outbox-commit");
        var configuration = BuildConfiguration(queueName);

        await using var host = await MessagingTestHost.StartAsync(services =>
            ConfigureOutboxHost(services, configuration, "outbox-commit-test"));

        var bus = host.Services.GetRequiredService<IBus>();
        await bus.Subscribe<OutboxProbeEvent>();
        await Task.Delay(TimeSpan.FromMilliseconds(300));

        var probeName = $"commit-{Guid.NewGuid():N}";
        OutboxProbeEntity persistedProbe;

        await using (var scope = host.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InventoryOutboxProbeDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            persistedProbe = new OutboxProbeEntity(probeName);
            db.Probes.Add(persistedProbe);
            await publisher.PublishAsync(new OutboxProbeEvent(probeName));
            await unitOfWork.SaveChangesAsync();
        }

        var handled = await OutboxProbeEventHandler.WaitForHandledAsync(TimeSpan.FromSeconds(15));
        handled.Should().BeTrue("the outbox forwarder must publish to RabbitMQ after commit");

        await using var readContext = CreateAppProbeContext();
        var reloaded = await readContext.Probes.SingleOrDefaultAsync(probe => probe.Name == probeName);
        reloaded.Should().NotBeNull();
        reloaded!.Id.Should().Be(persistedProbe.Id);
    }

    [Fact]
    public async Task RollbackWithoutCommit_LeavesNoProbeRow_NoOutboxRow_AndHandlerDoesNotReceiveEvent()
    {
        OutboxProbeEventHandler.Reset();

        var queueName = MessagingTestConfiguration.UniqueQueueName("outbox-rollback");
        var configuration = BuildConfiguration(queueName);

        await using var host = await MessagingTestHost.StartAsync(services =>
            ConfigureOutboxHost(services, configuration, "outbox-rollback-test"));

        var bus = host.Services.GetRequiredService<IBus>();
        await bus.Subscribe<OutboxProbeEvent>();
        await Task.Delay(TimeSpan.FromMilliseconds(300));

        var probeName = $"rollback-{Guid.NewGuid():N}";

        var outboxCountBefore = await CountOutboxRowsAsync();

        await using (var scope = host.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InventoryOutboxProbeDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();

            db.Probes.Add(new OutboxProbeEntity(probeName));
            await publisher.PublishAsync(new OutboxProbeEvent(probeName));
            // Scope ends without SaveChangesAsync — staged events must not reach the broker.
        }

        var handled = await OutboxProbeEventHandler.WaitForHandledAsync(TimeSpan.FromSeconds(3));
        handled.Should().BeFalse("rollback must not publish through the outbox forwarder");

        await using var readContext = CreateAppProbeContext();
        (await readContext.Probes.AnyAsync(probe => probe.Name == probeName))
            .Should().BeFalse("the probe row must not persist when the unit of work rolls back");

        var outboxCountAfter = await CountOutboxRowsAsync();
        outboxCountAfter.Should().Be(
            outboxCountBefore,
            "staged events that never reached SaveChanges must not remain in inventory.outbox");
    }

    [Fact]
    public async Task SaveChangesFailure_RollsBackProbeAndOutbox_AndHandlerDoesNotReceiveEvent()
    {
        OutboxProbeEventHandler.Reset();

        var queueName = MessagingTestConfiguration.UniqueQueueName("outbox-fail");
        var configuration = BuildConfiguration(queueName);

        await using var host = await MessagingTestHost.StartAsync(services =>
            ConfigureOutboxHost(services, configuration, "outbox-fail-test"));

        var bus = host.Services.GetRequiredService<IBus>();
        await bus.Subscribe<OutboxProbeEvent>();
        await Task.Delay(TimeSpan.FromMilliseconds(300));

        var probeName = $"force-rollback-{Guid.NewGuid():N}";
        var outboxCountBefore = await CountOutboxRowsAsync();

        await using (var scope = host.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InventoryOutboxProbeDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            db.Probes.Add(new OutboxProbeEntity(probeName));
            await publisher.PublishAsync(new OutboxProbeEvent(probeName));

            var saving = async () => await unitOfWork.SaveChangesAsync();
            await saving.Should().ThrowAsync<DbUpdateException>(
                "the CHECK constraint must fail after UseOutbox/Publish enlisted the event in the same transaction");
        }

        var handled = await OutboxProbeEventHandler.WaitForHandledAsync(TimeSpan.FromSeconds(3));
        handled.Should().BeFalse("a rolled-back outbox insert must never reach the broker");

        await using var readContext = CreateAppProbeContext();
        (await readContext.Probes.AnyAsync(probe => probe.Name == probeName))
            .Should().BeFalse("the probe row must roll back with the failed SaveChanges");

        var outboxCountAfter = await CountOutboxRowsAsync();
        outboxCountAfter.Should().Be(
            outboxCountBefore,
            "outbox rows written in the failed transaction must not remain in inventory.outbox");
    }

    private static void ConfigureOutboxHost(
        IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        services.AddSbaCarsPersistenceOptions(configuration);
        services.AddDbContext<InventoryOutboxProbeDbContext>((provider, options) =>
        {
            var persistence = provider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            options.UseSbaCarsNpgsql(persistence.ConnectionString, InventoryOutboxProbeDbContext.Schema);
        });
        services.AddEfUnitOfWork<InventoryOutboxProbeDbContext>();
        services.AddSbaCarsMessaging(configuration, serviceName, InventoryDbContext.Schema);
        services.AddRebusHandler<OutboxProbeEventHandler>();
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

    private InventoryOutboxProbeDbContext CreateAppProbeContext()
    {
        var appConnectionString = _postgresFixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw");
        var optionsBuilder = new DbContextOptionsBuilder<InventoryOutboxProbeDbContext>();
        optionsBuilder.UseSbaCarsNpgsql(appConnectionString, InventoryOutboxProbeDbContext.Schema);
        return new InventoryOutboxProbeDbContext(optionsBuilder.Options);
    }

    private async Task<long> CountOutboxRowsAsync()
    {
        await using var connection = new NpgsqlConnection(
            _postgresFixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM inventory.outbox;";
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    [IntegrationEvent("test.messaging-outbox-probe")]
    public sealed record OutboxProbeEvent(string ProbeName);

    public sealed class OutboxProbeEventHandler : IHandleMessages<OutboxProbeEvent>
    {
        private static TaskCompletionSource _handledSignal =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public static void Reset()
        {
            _handledSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task Handle(OutboxProbeEvent message)
        {
            _handledSignal.TrySetResult();
            return Task.CompletedTask;
        }

        public static async Task<bool> WaitForHandledAsync(TimeSpan timeout)
        {
            var completed = await Task.WhenAny(_handledSignal.Task, Task.Delay(timeout));
            return completed == _handledSignal.Task;
        }
    }
}
