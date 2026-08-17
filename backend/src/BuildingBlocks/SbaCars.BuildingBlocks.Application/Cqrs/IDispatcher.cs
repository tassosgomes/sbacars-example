namespace SbaCars.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Dispatches commands and queries to handlers resolved by their closed CLR types.
/// </summary>
public interface IDispatcher
{
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);

    Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
}
