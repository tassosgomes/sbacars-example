using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SbaCars.BuildingBlocks.Messaging;
using SbaCars.BuildingBlocks.Messaging.HealthChecks;

namespace SbaCars.Messaging.IntegrationTests;

/// <summary>
/// D10: <c>/health/ready</c>'s RabbitMQ leg reports the broker's real reachability — <c>Healthy</c>
/// against a live broker, <c>Unhealthy</c> (not a hang) against a dead one.
/// </summary>
[Collection(SbaCarsRabbitMqCollection.Name)]
public sealed class RabbitMqReadinessHealthCheckTests
{
    private readonly SbaCarsRabbitMqFixture _fixture;

    public RabbitMqReadinessHealthCheckTests(SbaCarsRabbitMqFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AgainstALiveBroker_TheCheckReportsHealthy()
    {
        var queueName = MessagingTestConfiguration.UniqueQueueName("health-ok");
        var configuration = MessagingTestConfiguration.Build(_fixture, queueName);

        await using var host = await MessagingTestHost.StartAsync(services =>
        {
            services.AddSbaCarsMessaging(configuration, "health-ok-test");
            services.AddHealthChecks().AddSbaCarsRabbitMqReadinessCheck("test");
        });

        var healthCheckService = host.Services.GetRequiredService<HealthCheckService>();
        var report = await healthCheckService.CheckHealthAsync();

        report.Status.Should().Be(HealthStatus.Healthy);
        report.Entries.Should().ContainKey("rabbitmq")
            .WhoseValue.Status.Should().Be(HealthStatus.Healthy);
    }

    /// <remarks>
    /// The dead-port scenario also proves the check does not trade a broken broker for a hung
    /// request: <see cref="RabbitMqReadinessHealthCheck"/> lets
    /// <c>IConnectionFactory.CreateConnectionAsync</c>'s own exception surface as <c>Unhealthy</c>
    /// rather than swallowing it into an indefinite wait, so this test bounds its own wait with a
    /// short timeout and asserts the call actually returns within it.
    /// </remarks>
    [Fact]
    public async Task AgainstADeadPort_TheCheckReportsUnhealthy_WithoutHanging()
    {
        var queueName = MessagingTestConfiguration.UniqueQueueName("health-dead");
        var configuration = MessagingTestConfiguration.Build(_fixture, queueName, new Dictionary<string, string?>
        {
            // Port 1 is a reserved, never-listening TCP port — nothing will ever accept this
            // connection, which is exactly the "broker unreachable" scenario this test targets.
            ["Messaging:ConnectionString"] = "amqp://sbacars:sbacars_dev_pw@localhost:1/",
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSbaCarsMessaging(configuration, "health-dead-test");
        services.AddHealthChecks().AddSbaCarsRabbitMqReadinessCheck("test");

        // Only the health check probe is exercised here — not the bus itself (a dead connection
        // string would also make AddSbaCarsMessaging's own hosted service fail to start, which is a
        // separate concern from what this test is about).
        await using var provider = services.BuildServiceProvider();
        var healthCheckService = provider.GetRequiredService<HealthCheckService>();

        // A dead port's TCP connect attempt is not necessarily refused instantly on every network
        // stack (confirmed empirically in this environment: it can hang past 10s with no timeout
        // applied) — bounding the wait is the caller's responsibility, exactly like a real readiness
        // probe's own request timeout, and the health check must honor cancellation instead of
        // ignoring it. This is the "timeout curto" the B1 test spec calls for.
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var checkTask = healthCheckService.CheckHealthAsync(timeoutCts.Token);
        var winner = await Task.WhenAny(checkTask, Task.Delay(TimeSpan.FromSeconds(10)));

        winner.Should().Be(checkTask, "the check must return within a bounded time, not hang indefinitely against an unreachable broker");

        var report = await checkTask;
        report.Status.Should().Be(HealthStatus.Unhealthy);
        report.Entries.Should().ContainKey("rabbitmq")
            .WhoseValue.Status.Should().Be(HealthStatus.Unhealthy);
    }
}
