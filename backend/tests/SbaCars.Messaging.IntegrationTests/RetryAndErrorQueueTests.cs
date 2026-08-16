using Microsoft.Extensions.DependencyInjection;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Retry.Simple;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Messaging;
using SbaCars.Contracts;

namespace SbaCars.Messaging.IntegrationTests;

/// <summary>
/// The retry/second-level-retry/error-queue half of B1's scope (§6.3, D6). A handler that always
/// throws must, after <c>MaxDeliveryAttempts</c> first-level attempts, either be intercepted by an
/// <c>IHandleMessages&lt;IFailed&lt;T&gt;&gt;</c> handler (second-level retry) or, absent one, land
/// the message in the error queue — both are asserted against the real broker, not against Rebus'
/// in-process retry bookkeeping.
/// </summary>
/// <remarks>
/// <c>MaxDeliveryAttempts</c> is set to 2 in every test here (§6.3.1: "retry ilimitado queima a cota
/// de mensagens" — a test that waits out 5 attempts with backoff would be slow without proving
/// anything the 2-attempt version doesn't already prove).
/// </remarks>
[Collection(SbaCarsRabbitMqCollection.Name)]
public sealed class RetryAndErrorQueueTests
{
    private readonly SbaCarsRabbitMqFixture _fixture;

    public RetryAndErrorQueueTests(SbaCarsRabbitMqFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AHandlerThatAlwaysThrows_SendsTheMessageToTheErrorQueue_AfterMaxDeliveryAttempts()
    {
        var queueName = MessagingTestConfiguration.UniqueQueueName("retry-err");
        var configuration = MessagingTestConfiguration.Build(_fixture, queueName, new Dictionary<string, string?>
        {
            ["Messaging:MaxDeliveryAttempts"] = "2",
        });

        // Deliberately no IHandleMessages<IFailed<T>> registered: nothing intercepts the failure
        // before Rebus' own retry strategy dead-letters the message.
        await using var host = await MessagingTestHost.StartAsync(services =>
        {
            services.AddSbaCarsMessaging(configuration, "retry-error-test");
            services.AddRebusHandler<AlwaysThrowsHandler>();
        });

        var bus = host.Services.GetRequiredService<IBus>();
        await bus.Subscribe<AlwaysThrowsEvent>();
        await Task.Delay(TimeSpan.FromMilliseconds(300));

        var publisher = host.Services.GetRequiredService<IIntegrationEventPublisher>();
        await publisher.PublishAsync(new AlwaysThrowsEvent());

        using var management = new RabbitMqManagementClient(
            _fixture.ManagementApiBaseUri, _fixture.ManagementUsername, _fixture.ManagementPassword);

        var errorQueueMessageCount = await PollForErrorQueueMessageCountAsync(management, $"{queueName}.error");

        errorQueueMessageCount.Should().Be(1,
            "after MaxDeliveryAttempts (2) first-level attempts with no IFailed<T> handler to intercept, " +
            "Rebus must dead-letter the message onto the error queue");
        AlwaysThrowsHandler.AttemptCount.Should().Be(2, "the handler must be invoked exactly MaxDeliveryAttempts times, not more");
    }

    [Fact]
    public async Task WithSecondLevelRetriesEnabled_AnIFailedHandler_ReceivesTheMessageBeforeTheErrorQueueDoes()
    {
        var queueName = MessagingTestConfiguration.UniqueQueueName("retry-2nd");
        var configuration = MessagingTestConfiguration.Build(_fixture, queueName, new Dictionary<string, string?>
        {
            ["Messaging:MaxDeliveryAttempts"] = "2",
            ["Messaging:SecondLevelRetriesEnabled"] = "true",
        });

        await using var host = await MessagingTestHost.StartAsync(services =>
        {
            services.AddSbaCarsMessaging(configuration, "second-level-retry-test");
            services.AddRebusHandler<AlwaysThrowsHandler>();
            services.AddRebusHandler<FailedEventHandler>();
        });

        var bus = host.Services.GetRequiredService<IBus>();
        await bus.Subscribe<AlwaysThrowsEvent>();
        await Task.Delay(TimeSpan.FromMilliseconds(300));

        var publisher = host.Services.GetRequiredService<IIntegrationEventPublisher>();
        await publisher.PublishAsync(new AlwaysThrowsEvent());

        var handled = await FailedEventHandler.WaitForHandledAsync(TimeSpan.FromSeconds(10));
        handled.Should().BeTrue("the IFailed<T> handler must run once first-level retries are exhausted");
        FailedEventHandler.ObservedErrorDescription.Should().Contain(AlwaysThrowsHandler.Boom);

        using var management = new RabbitMqManagementClient(
            _fixture.ManagementApiBaseUri, _fixture.ManagementUsername, _fixture.ManagementPassword);

        // A successful IFailed<T> handler is what stops the message from also being dead-lettered —
        // give the queue a moment to settle, then assert it stayed empty.
        await Task.Delay(TimeSpan.FromSeconds(1));
        var errorQueueMessageCount = await GetQueueMessageCountAsync(management, $"{queueName}.error");
        errorQueueMessageCount.Should().Be(0,
            "a successful IFailed<T> handler must intercept the message before it reaches the error queue");
    }

    private static async Task<int> PollForErrorQueueMessageCountAsync(RabbitMqManagementClient management, string errorQueueName)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(15);
        var lastCount = 0;

        while (DateTimeOffset.UtcNow < deadline)
        {
            lastCount = await GetQueueMessageCountAsync(management, errorQueueName);
            if (lastCount > 0)
            {
                return lastCount;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        return lastCount;
    }

    private static async Task<int> GetQueueMessageCountAsync(RabbitMqManagementClient management, string queueName)
    {
        var queues = await management.GetQueuesAsync(CancellationToken.None);
        var queue = queues.EnumerateArray().FirstOrDefault(q => q.GetProperty("name").GetString() == queueName);

        // Just like /api/connections (see ConnectionBudgetTests), a freshly-declared queue's
        // detailed stats (including "messages") are populated by the management plugin's periodic
        // stats aggregator, not present the instant the queue is declared — a queue that exists but
        // has never had a sample collected yet simply omits the field, and that means 0 messages,
        // not a missing/undefined queue.
        return queue.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
               queue.TryGetProperty("messages", out var messages)
            ? messages.GetInt32()
            : 0;
    }

    [IntegrationEvent("test.always-throws")]
    public sealed class AlwaysThrowsEvent;

    public sealed class AlwaysThrowsHandler : IHandleMessages<AlwaysThrowsEvent>
    {
        public const string Boom = "boom-from-retry-test";

        private static int _attemptCount;

        public static int AttemptCount => _attemptCount;

        public Task Handle(AlwaysThrowsEvent message)
        {
            Interlocked.Increment(ref _attemptCount);
            throw new InvalidOperationException(Boom);
        }
    }

    public sealed class FailedEventHandler : IHandleMessages<IFailed<AlwaysThrowsEvent>>
    {
        private static readonly TaskCompletionSource HandledSignal =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public static string? ObservedErrorDescription { get; private set; }

        public Task Handle(IFailed<AlwaysThrowsEvent> message)
        {
            ObservedErrorDescription = message.ErrorDescription;
            HandledSignal.TrySetResult();
            return Task.CompletedTask;
        }

        public static async Task<bool> WaitForHandledAsync(TimeSpan timeout)
        {
            var completed = await Task.WhenAny(HandledSignal.Task, Task.Delay(timeout));
            return completed == HandledSignal.Task;
        }
    }
}
