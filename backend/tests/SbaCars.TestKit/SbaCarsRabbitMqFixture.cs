using Testcontainers.RabbitMq;
using Xunit;

namespace SbaCars.TestKit;

/// <summary>
/// One <c>rabbitmq:4.2-management-alpine</c> container — the same tag <c>docker-compose.yml</c>
/// uses for its own <c>rabbitmq</c> service (D12 of the B1 task spec) — for every messaging
/// integration test that needs a real broker to prove topology, retries and trace propagation
/// against (§6, §9 of the architecture plan). Reusing the exact image tag the local environment
/// runs is the same principle <see cref="SbaCarsPostgresFixture"/> already applies to
/// <c>postgres:18</c>: what proves the behavior in CI is what actually runs in Development, not a
/// nearby stand-in.
/// </summary>
/// <remarks>
/// Lives in <c>SbaCars.TestKit</c> from the moment it is written, for the same reason
/// <see cref="SbaCarsPostgresFixture"/>'s own remarks give for its A9 move: §3.1 of the
/// architecture plan names "fixtures Testcontainers" explicitly as TestKit content. That is the
/// justification here too, not §3.3's "second real consumer" rule — this fixture starts with
/// exactly one consumer, <c>SbaCars.Messaging.IntegrationTests</c>, just as
/// <see cref="SbaCarsPostgresFixture"/> itself did before A9.
/// </remarks>
public sealed class SbaCarsRabbitMqFixture : IAsyncLifetime
{
    private const string AdminUsername = "sbacars";
    private const string AdminPassword = "sbacars_dev_pw";
    private const ushort ManagementPort = 15672;

    public RabbitMqContainer Container { get; }

    public SbaCarsRabbitMqFixture()
    {
        Container = new RabbitMqBuilder("rabbitmq:4.2-management-alpine")
            .WithUsername(AdminUsername)
            .WithPassword(AdminPassword)
            // The AMQP port (5672) is already exposed/bound by RabbitMqBuilder's own defaults; the
            // management API (15672) is specific to the "-management" image variant and needs its
            // own binding — tests reach it over HTTP to verify topology/connections against the
            // broker itself, not against this process' in-memory configuration objects.
            .WithPortBinding(ManagementPort, assignRandomHostPort: true)
            .Build();
    }

    public Task InitializeAsync() => Container.StartAsync();

    public Task DisposeAsync() => Container.DisposeAsync().AsTask();

    /// <summary>AMQP connection string (<c>amqp://sbacars:...@host:port/</c>) for this container.</summary>
    public string AmqpConnectionString => Container.GetConnectionString();

    /// <summary>Base URI of the RabbitMQ management HTTP API for this container.</summary>
    public Uri ManagementApiBaseUri => new($"http://{Container.Hostname}:{Container.GetMappedPublicPort(ManagementPort)}");

    public string ManagementUsername => AdminUsername;

    public string ManagementPassword => AdminPassword;
}
