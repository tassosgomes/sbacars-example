using Rebus.Transport;

namespace SbaCars.BuildingBlocks.Messaging.Tests;

/// <summary>
/// The minimal <see cref="ITransactionContext"/> every <c>OutgoingStepContext</c>/
/// <c>IncomingStepContext</c> construction needs (Rebus' own concrete implementation,
/// <c>Rebus.Transport.TransactionContext</c>, is internal to the <c>Rebus</c> assembly). None of the
/// pipeline steps under test read or write transaction-context state — they only load/save items on
/// the <c>StepContext</c> itself — so every member here is a no-op.
/// </summary>
internal sealed class FakeTransactionContext : ITransactionContext
{
    public System.Collections.Concurrent.ConcurrentDictionary<string, object> Items { get; } = new();

    public void OnCommit(Func<ITransactionContext, Task> commitAction)
    {
    }

    public void OnRollback(Func<ITransactionContext, Task> rollbackAction)
    {
    }

    public void OnAck(Func<ITransactionContext, Task> ackAction)
    {
    }

    public void OnNack(Func<ITransactionContext, Task> nackAction)
    {
    }

    public void OnDisposed(Action<ITransactionContext> disposedAction)
    {
    }

    public void SetResult(bool commit, bool ack)
    {
    }

    public void Dispose()
    {
    }
}
