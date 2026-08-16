using Rebus.Messages;
using Rebus.Pipeline;
using SbaCars.BuildingBlocks.Messaging.Inbox;

namespace SbaCars.BuildingBlocks.Messaging.Tests;

/// <summary>
/// Proves B3: <see cref="InboxDeduplicationIncomingStep"/> implements process-then-record idempotency
/// on <c>(message_id, consumer)</c> without requiring Postgres in unit tests.
/// </summary>
public sealed class InboxDeduplicationIncomingStepTests
{
    private const string Consumer = "inventory-service";
    private const string MessageId = "11111111-2222-3333-4444-555555555555";

    [Fact]
    public async Task AlreadyProcessedMessage_DoesNotCallNext()
    {
        var store = new FakeInboxStore { IsProcessedResult = true };
        var nextCalled = false;

        await RunStepAsync(store, next: () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeFalse();
        store.RecordCalls.Should().BeEmpty();
    }

    [Fact]
    public async Task FirstDelivery_CallsNext_ThenRecordsInbox()
    {
        var store = new FakeInboxStore();
        var nextCalled = false;

        await RunStepAsync(store, next: () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
        store.RecordCalls.Should().ContainSingle()
            .Which.Should().Be((MessageId, Consumer));
    }

    [Fact]
    public async Task WhenNextThrows_InboxIsNotRecorded()
    {
        var store = new FakeInboxStore();

        var act = () => RunStepAsync(store, next: () => throw new InvalidOperationException("handler failed"));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("handler failed");
        store.RecordCalls.Should().BeEmpty();
    }

    [Fact]
    public async Task BenignRaceOnInsert_DoesNotFailTheAck()
    {
        var store = new FakeInboxStore { TryRecordProcessedResult = false };

        var act = () => RunStepAsync(store, next: () => Task.CompletedTask);

        await act.Should().NotThrowAsync();
        store.RecordCalls.Should().ContainSingle();
    }

    private static async Task RunStepAsync(FakeInboxStore store, Func<Task> next)
    {
        var headers = new Dictionary<string, string> { [Headers.MessageId] = MessageId };
        var transportMessage = new TransportMessage(headers, []);
        var context = new IncomingStepContext(transportMessage, new FakeTransactionContext());

        var step = new InboxDeduplicationIncomingStep(store, Consumer);
        await step.Process(context, next);
    }

    private sealed class FakeInboxStore : IInboxStore
    {
        public bool IsProcessedResult { get; init; }

        public bool TryRecordProcessedResult { get; init; } = true;

        public List<(string MessageId, string Consumer)> RecordCalls { get; } = [];

        public Task<bool> IsProcessedAsync(
            string messageId,
            string consumer,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(IsProcessedResult);

        public Task<bool> TryRecordProcessedAsync(
            string messageId,
            string consumer,
            CancellationToken cancellationToken = default)
        {
            RecordCalls.Add((messageId, consumer));
            return Task.FromResult(TryRecordProcessedResult);
        }
    }
}
