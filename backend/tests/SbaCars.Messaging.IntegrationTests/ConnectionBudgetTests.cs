using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SbaCars.BuildingBlocks.Messaging;
using SbaCars.BuildingBlocks.Messaging.HealthChecks;

namespace SbaCars.Messaging.IntegrationTests;

/// <summary>
/// §6.3.1 of the architecture plan says, literally: "Rebus abre de uma a duas conexões por processo
/// (confirmar na B1; orçamos duas)." This is that confirmation — not an estimate anymore. D10's
/// design (one long-lived bus connection plus one long-lived readiness-probe connection, both
/// reused rather than opened per poll) is what this test measures against the real broker.
/// </summary>
/// <remarks>
/// Why this number matters beyond this one test run: §6.3.1 sizes the whole topology against
/// CloudAMQP's 40-connection plan ceiling across four services, and every row of its budget table is
/// this number times a replica count. At 2 connections/process: 8 steady-state at one replica each,
/// 16 at two, and up to 32 mid-rolling-update, where <c>start-first</c> has the old and the new
/// replica alive at once. That last row is why §6.3.1 makes <b>two replicas per service the ceiling
/// of the dev environment</b> — three replicas would be 4 × 3 × 2 = 24 steady-state but 48 during a
/// deploy, which overruns the plan. If this number ever drifts to 3 (a connection leak, a health
/// check that stops reusing its probe, or a change that opens a connection per poll instead of
/// once), even the sanctioned two-replica deploy becomes 48 and starts failing to connect in
/// production — which is exactly why this is asserted here instead of only documented in prose.
/// </remarks>
[Collection(SbaCarsRabbitMqCollection.Name)]
public sealed class ConnectionBudgetTests
{
    private readonly SbaCarsRabbitMqFixture _fixture;

    public ConnectionBudgetTests(SbaCarsRabbitMqFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ABusPlusItsReadinessProbe_OpenExactlyTwoConnectionsToTheBroker()
    {
        var queueName = MessagingTestConfiguration.UniqueQueueName("budget");
        var configuration = MessagingTestConfiguration.Build(_fixture, queueName);

        await using var host = await MessagingTestHost.StartAsync(services =>
        {
            services.AddSbaCarsMessaging(configuration, "budget-test");
            services.AddHealthChecks().AddSbaCarsRabbitMqReadinessCheck("test");
        });

        // D10: the probe's connection opens lazily, on the first check — not merely on registration.
        // Running the check once is what makes the second connection exist to be counted.
        var healthCheckService = host.Services.GetRequiredService<HealthCheckService>();
        var report = await healthCheckService.CheckHealthAsync();
        report.Status.Should().Be(HealthStatus.Healthy);

        using var management = new RabbitMqManagementClient(
            _fixture.ManagementApiBaseUri, _fixture.ManagementUsername, _fixture.ManagementPassword);

        // RabbitMQ's management plugin reports /api/connections from its own periodic stats
        // aggregator, not a live query of the broker's connection table (unlike /api/queues and
        // /api/exchanges, which TopologyDeclarationTests reads straight off the live object list) —
        // a connection opened an instant ago can legitimately be invisible here for a beat. Polling
        // is what the plugin's own design requires, not a workaround for flakiness.
        var thisProcessConnections = await PollUntilStableAsync(async () =>
        {
            var connections = await management.GetConnectionsAsync(CancellationToken.None);
            return connections.EnumerateArray().Where(connection => BelongsToThisTest(connection, queueName)).ToArray();
        });

        thisProcessConnections.Should().HaveCount(2,
            "§6.3.1 budgets exactly 1 (Rebus bus) + 1 (readiness probe) connections per process — " +
            $"found: {string.Join(", ", thisProcessConnections.Select(DescribeConnection))}");
    }

    /// <summary>
    /// Polls <paramref name="query"/> until it returns the expected 2 connections or a 10-second
    /// budget runs out, returning whatever the last poll saw either way — the final
    /// <c>HaveCount(2)</c> assertion is still what decides pass/fail, this only gives the
    /// management plugin's stats aggregator room to catch up first.
    /// </summary>
    private static async Task<JsonElement[]> PollUntilStableAsync(Func<Task<JsonElement[]>> query)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        JsonElement[] lastResult = [];

        while (DateTimeOffset.UtcNow < deadline)
        {
            lastResult = await query();
            if (lastResult.Length == 2)
            {
                return lastResult;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        return lastResult;
    }

    private static bool BelongsToThisTest(JsonElement connection, string queueName)
    {
        if (!connection.TryGetProperty("client_properties", out var clientProperties))
        {
            return false;
        }

        // The bus connection carries Rebus' own "InputQueue" client property (set by Rebus.RabbitMq
        // itself); the readiness probe carries no such property, only the connection_name D10 gives
        // it ("{InputQueueName}/health-probe").
        if (clientProperties.TryGetProperty("InputQueue", out var inputQueue) &&
            inputQueue.GetString() == queueName)
        {
            return true;
        }

        return clientProperties.TryGetProperty("connection_name", out var connectionName) &&
               connectionName.GetString() == $"{queueName}/health-probe";
    }

    private static string DescribeConnection(JsonElement connection) =>
        connection.TryGetProperty("client_properties", out var clientProperties) &&
        clientProperties.TryGetProperty("connection_name", out var connectionName)
            ? connectionName.GetString() ?? "(unnamed)"
            : "(no client_properties)";
}
