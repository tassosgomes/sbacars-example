using SbaCars.BuildingBlocks.Messaging;
using SbaCars.BuildingBlocks.Messaging.Topology;

namespace SbaCars.Messaging.IntegrationTests;

/// <summary>
/// The "declara a topologia" half of B1's readiness criterion (§12): starting a real bus against a
/// real broker must produce, on the broker itself, exactly the topology D3/§6.3 describe — one
/// durable topic exchange (<see cref="MessagingTopology.TopicExchangeName"/>), one durable direct
/// exchange (<see cref="MessagingTopology.DirectExchangeName"/>), one durable input queue per
/// service, and one durable error queue per service.
/// </summary>
/// <remarks>
/// Asserts against the RabbitMQ management HTTP API, not against <c>MessagingOptions</c> or any
/// other in-memory configuration object this process holds — a correctly-configured
/// <see cref="RabbitMqOptionsBuilder"/> that never actually reaches the broker (wrong connection
/// string, wrong vhost, a swallowed exception during declaration) is exactly the failure mode this
/// test needs to catch, and only the broker's own view of its topology can catch it.
/// </remarks>
[Collection(SbaCarsRabbitMqCollection.Name)]
public sealed class TopologyDeclarationTests
{
    private readonly SbaCarsRabbitMqFixture _fixture;

    public TopologyDeclarationTests(SbaCarsRabbitMqFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StartingTheBus_DeclaresTheExpectedTopologyOnTheRealBroker()
    {
        var queueName = MessagingTestConfiguration.UniqueQueueName("topology");
        var configuration = MessagingTestConfiguration.Build(_fixture, queueName);

        // Starting the bus alone — no Subscribe, no Publish — is enough: exchange/queue declaration
        // happens as part of bus startup, not lazily on first use. Verified empirically while writing
        // this test (a bus started with zero subscriptions already had its queues on the broker).
        await using var host = await MessagingTestHost.StartAsync(services =>
            services.AddSbaCarsMessaging(configuration, "topology-test"));

        using var management = new RabbitMqManagementClient(
            _fixture.ManagementApiBaseUri, _fixture.ManagementUsername, _fixture.ManagementPassword);

        var exchanges = await management.GetExchangesAsync(CancellationToken.None);

        var topicExchange = exchanges.EnumerateArray()
            .Should().ContainSingle(e => e.GetProperty("name").GetString() == MessagingTopology.TopicExchangeName)
            .Subject;
        topicExchange.GetProperty("type").GetString().Should().Be("topic");
        topicExchange.GetProperty("durable").GetBoolean().Should().BeTrue();

        var directExchange = exchanges.EnumerateArray()
            .Should().ContainSingle(e => e.GetProperty("name").GetString() == MessagingTopology.DirectExchangeName)
            .Subject;
        directExchange.GetProperty("type").GetString().Should().Be("direct");
        directExchange.GetProperty("durable").GetBoolean().Should().BeTrue();

        var queues = await management.GetQueuesAsync(CancellationToken.None);

        var inputQueue = queues.EnumerateArray()
            .Should().ContainSingle(q => q.GetProperty("name").GetString() == queueName)
            .Subject;
        inputQueue.GetProperty("durable").GetBoolean().Should().BeTrue();

        var errorQueue = queues.EnumerateArray()
            .Should().ContainSingle(q => q.GetProperty("name").GetString() == $"{queueName}.error")
            .Subject;
        errorQueue.GetProperty("durable").GetBoolean().Should().BeTrue();
    }
}
