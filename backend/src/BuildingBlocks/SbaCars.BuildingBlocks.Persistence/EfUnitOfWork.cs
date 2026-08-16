using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Rebus.Bus;
using Rebus.Config.Outbox;
using Rebus.Transport;
using SbaCars.BuildingBlocks.Application;

namespace SbaCars.BuildingBlocks.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IUnitOfWork"/>, <see cref="IOutboxTransaction"/>, and
/// <see cref="IOutboxMessageStaging"/> (§6.2): staged integration events and EF changes commit
/// together inside one execution-strategy scope — <c>UseOutbox</c>, <see cref="IBus.Publish"/>,
/// <c>SaveChangesAsync</c>, <c>CompleteAsync</c>, then <c>CommitAsync</c>.
/// </summary>
/// <remarks>
/// Staging exists because <see cref="SbaCarsNpgsqlOptionsExtensions.UseSbaCarsNpgsql"/> enables
/// retry-on-failure: an explicit transaction must begin and end inside a single
/// <see cref="DatabaseFacade.CreateExecutionStrategy"/>.<c>ExecuteAsync</c> call. Use cases still
/// call <c>PublishAsync</c> then <c>SaveChangesAsync</c>; the publisher stages on
/// <see cref="IOutboxMessageStaging"/>, and this class publishes from the staged list once the
/// strategy scope opens the transaction.
/// <para>
/// Staged events are cleared only after a successful commit. Clearing them before
/// <c>CommitAsync</c> would drop the events on a transient retry: the strategy re-enters this
/// method with an empty list and would persist the aggregate without republishing.
/// </para>
/// </remarks>
public sealed class EfUnitOfWork<TContext> : IUnitOfWork, IOutboxTransaction, IOutboxMessageStaging, IAsyncDisposable
    where TContext : SbaCarsDbContext
{
    private readonly TContext _context;
    private readonly IBus _bus;
    private readonly List<object> _stagedIntegrationEvents = [];
    private bool _completed;

    public EfUnitOfWork(TContext context, IBus bus)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bus);
        _context = context;
        _bus = bus;
    }

    /// <inheritdoc />
    public Task EnsureOpenAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public void Stage(object integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        _stagedIntegrationEvents.Add(integrationEvent);
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_stagedIntegrationEvents.Count == 0)
        {
            return await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();

        return await _context.Database.CreateExecutionStrategy()
            .ExecuteAsync(CommitWithOutboxAsync, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<int> CommitWithOutboxAsync(CancellationToken cancellationToken)
    {
        await _context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        await using var efTransaction = await _context.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        using var rebusScope = new RebusTransactionScope();
        var npgsqlConnection = (NpgsqlConnection)_context.Database.GetDbConnection();
        var npgsqlTransaction = (NpgsqlTransaction)efTransaction.GetDbTransaction();
        rebusScope.UseOutbox(npgsqlConnection, npgsqlTransaction);

        foreach (var integrationEvent in _stagedIntegrationEvents)
        {
            await _bus.Publish(integrationEvent).ConfigureAwait(false);
        }

        var affected = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await rebusScope.CompleteAsync().ConfigureAwait(false);
        await efTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        _stagedIntegrationEvents.Clear();
        _completed = true;
        return affected;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            _stagedIntegrationEvents.Clear();
        }

        return ValueTask.CompletedTask;
    }
}
