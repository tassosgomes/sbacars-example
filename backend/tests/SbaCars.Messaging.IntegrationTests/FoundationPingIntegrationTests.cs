using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Messages;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Messaging;
using SbaCars.BuildingBlocks.Messaging.Tracing;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Catalog.Api.Messaging.Foundation;
using SbaCars.Catalog.Infrastructure;
using SbaCars.Contracts.Foundation.V1;
using SbaCars.Inventory.Infrastructure;
using SbaCars.TestKit;

namespace SbaCars.Messaging.IntegrationTests;

/// <summary>
/// B5 readiness (§6.5): inventory publishes <c>foundation.ping</c> through the transactional outbox;
/// catalog consumes once, inbox deduplicates redelivery, and <c>traceparent</c> on the wire carries
/// the inventory publish span's trace-id.
/// </summary>
[Collection(SbaCarsRabbitMqCollection.Name)]
public sealed class FoundationPingIntegrationTests : IAsyncLifetime, IClassFixture<SbaCarsPostgresFixture>
{
    private const string InventoryServiceName = "inventory-service";
    private const string CatalogServiceName = "catalog-service";

    private readonly SbaCarsRabbitMqFixture _rabbitMqFixture;
    private readonly SbaCarsPostgresFixture _postgresFixture;

    public FoundationPingIntegrationTests(
        SbaCarsRabbitMqFixture rabbitMqFixture,
        SbaCarsPostgresFixture postgresFixture)
    {
        _rabbitMqFixture = rabbitMqFixture;
        _postgresFixture = postgresFixture;
    }

    public async Task InitializeAsync()
    {
        var ownerInventory = _postgresFixture.ConnectionStringFor("own_inventory", "own_inventory_dev_pw");
        var inventoryOptions = new DbContextOptionsBuilder<InventoryDbContext>();
        inventoryOptions.UseSbaCarsNpgsql(ownerInventory, InventoryDbContext.Schema);
        await using var inventoryContext = new InventoryDbContext(inventoryOptions.Options);
        await inventoryContext.Database.MigrateAsync();

        var ownerCatalog = _postgresFixture.ConnectionStringFor("own_catalog", "own_catalog_dev_pw");
        var catalogOptions = new DbContextOptionsBuilder<CatalogDbContext>();
        catalogOptions.UseSbaCarsNpgsql(ownerCatalog, CatalogDbContext.Schema);
        await using var catalogContext = new CatalogDbContext(catalogOptions.Options);
        await catalogContext.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task InventoryOutboxPublish_CatalogConsumesOnce_OnRedelivery_Deduped_WithCorrelatedTraceparent()
    {
        var exportedInventoryActivities = new List<Activity>();
        using var inventoryTracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(MessagingActivitySource.Name)
            .AddInMemoryExporter(exportedInventoryActivities)
            .Build();

        var catalogQueueName = MessagingTestConfiguration.UniqueQueueName("catalog-foundation");
        var inventoryQueueName = MessagingTestConfiguration.UniqueQueueName("inventory-foundation");

        var catalogConfiguration = BuildCatalogConfiguration(catalogQueueName);
        var inventoryConfiguration = BuildInventoryConfiguration(inventoryQueueName);

        var catalogReceipt = new FoundationPingReceipt();

        await using var catalogHost = await MessagingTestHost.StartAsync(services =>
            ConfigureCatalogHost(services, catalogConfiguration, catalogReceipt));

        await using var inventoryHost = await MessagingTestHost.StartAsync(services =>
            ConfigureInventoryHost(services, inventoryConfiguration));

        // Catalog's hosted service subscribes on start; allow the binding to land before publish.
        await Task.Delay(TimeSpan.FromMilliseconds(300));

        Activity? rootActivity;
        Guid publishedPingId;

        await using (var scope = inventoryHost.Services.CreateAsyncScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            using (rootActivity = MessagingActivitySource.Instance.StartActivity(
                "foundation-ping-integration-test-root",
                ActivityKind.Internal))
            {
                publishedPingId = Guid.NewGuid();
                await publisher.PublishAsync(
                    new FoundationPingIntegrationEvent(publishedPingId, DateTimeOffset.UtcNow));
                await unitOfWork.SaveChangesAsync();
            }
        }

        var handled = await catalogReceipt.WaitForHandleCountAsync(1, TimeSpan.FromSeconds(15));
        handled.Should().BeTrue("catalog must consume the outbox-forwarded foundation.ping once");

        catalogReceipt.LastPingId.Should().Be(publishedPingId);
        catalogReceipt.LastMessageId.Should().NotBeNullOrEmpty();
        catalogReceipt.ObservedTraceparent.Should().NotBeNullOrEmpty();

        var inboxCount = await CountCatalogInboxRowsAsync(catalogReceipt.LastMessageId!, CatalogServiceName);
        inboxCount.Should().Be(1, "exactly one (message_id, consumer) row must exist after a successful handle");

        var inventoryBus = inventoryHost.Services.GetRequiredService<IBus>();
        await inventoryBus.Publish(
            new FoundationPingIntegrationEvent(Guid.NewGuid(), DateTimeOffset.UtcNow),
            new Dictionary<string, string> { [Headers.MessageId] = catalogReceipt.LastMessageId! });

        await Task.Delay(TimeSpan.FromSeconds(2));

        catalogReceipt.HandleCount.Should().Be(1, "redelivery of the same rbs2-msg-id must not run the catalog handler again");

        rootActivity.Should().NotBeNull();
        var publishSpan = exportedInventoryActivities.Should()
            .ContainSingle(
                activity => activity.OperationName == "foundation.ping publish"
                    && activity.TraceId == rootActivity!.TraceId,
                "only the outbox-forwarded publish must share the test root trace — redelivery uses a separate publish span")
            .Subject;

        publishSpan.TraceId.Should().Be(rootActivity!.TraceId, "the publish span must be a child of the trace the test started");
        catalogReceipt.ObservedTraceparent.Should().Contain(publishSpan.TraceId.ToHexString());
        catalogReceipt.ObservedTraceparent.Should().Contain(publishSpan.SpanId.ToHexString());
    }

    private static void ConfigureInventoryHost(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSbaCarsPersistenceOptions(configuration);
        services.AddDbContext<InventoryDbContext>((provider, options) =>
        {
            var persistence = provider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            options.UseSbaCarsNpgsql(persistence.ConnectionString, InventoryDbContext.Schema);
        });
        services.AddEfUnitOfWork<InventoryDbContext>();
        services.AddSbaCarsMessaging(configuration, InventoryServiceName, InventoryDbContext.Schema);
    }

    private static void ConfigureCatalogHost(
        IServiceCollection services,
        IConfiguration configuration,
        FoundationPingReceipt receipt)
    {
        services.AddSbaCarsPersistenceOptions(configuration);
        services.AddDbContext<CatalogDbContext>((provider, options) =>
        {
            var persistence = provider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            options.UseSbaCarsNpgsql(persistence.ConnectionString, CatalogDbContext.Schema);
        });
        services.AddEfUnitOfWork<CatalogDbContext>();
        services.AddSbaCarsMessaging(configuration, CatalogServiceName, CatalogDbContext.Schema);
        services.AddSingleton(receipt);
        services.AddRebusHandler<FoundationPingHandler>();
        services.AddHostedService<FoundationPingSubscriptionHostedService>();
    }

    private IConfiguration BuildInventoryConfiguration(string queueName) =>
        MessagingTestConfiguration.Build(
            _rabbitMqFixture,
            queueName,
            new Dictionary<string, string?>
            {
                ["Persistence:ConnectionString"] =
                    _postgresFixture.ConnectionStringFor("svc_inventory", "svc_inventory_dev_pw"),
            });

    private IConfiguration BuildCatalogConfiguration(string queueName) =>
        MessagingTestConfiguration.Build(
            _rabbitMqFixture,
            queueName,
            new Dictionary<string, string?>
            {
                ["Persistence:ConnectionString"] =
                    _postgresFixture.ConnectionStringFor("svc_catalog", "svc_catalog_dev_pw"),
            });

    private async Task<long> CountCatalogInboxRowsAsync(string messageId, string consumer)
    {
        await using var connection = new NpgsqlConnection(
            _postgresFixture.ConnectionStringFor("svc_catalog", "svc_catalog_dev_pw"));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(*)
            FROM catalog.inbox_message
            WHERE message_id = @message_id
              AND consumer = @consumer;
            """;
        command.Parameters.AddWithValue("message_id", messageId);
        command.Parameters.AddWithValue("consumer", consumer);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }
}
