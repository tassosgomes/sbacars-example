using Microsoft.Extensions.DependencyInjection;

namespace SbaCars.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Minimal native CQRS dispatcher. It deliberately has no pipeline or string-based lookup;
/// handlers are resolved from the container using the concrete command/query type.
/// </summary>
public sealed class Dispatcher(IServiceProvider serviceProvider) : IDispatcher
{
    public Task<TResult> SendAsync<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        var handler = serviceProvider.GetRequiredService(handlerType);
        return InvokeAsync<TResult>(handlerType, handler, command, cancellationToken);
    }

    public Task<TResult> QueryAsync<TResult>(
        IQuery<TResult> query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        var handler = serviceProvider.GetRequiredService(handlerType);
        return InvokeAsync<TResult>(handlerType, handler, query, cancellationToken);
    }

    private static async Task<TResult> InvokeAsync<TResult>(
        Type handlerType,
        object handler,
        object request,
        CancellationToken cancellationToken)
    {
        var method = handlerType.GetMethod(nameof(ICommandHandler<ICommand<TResult>, TResult>.HandleAsync));
        if (method is null)
        {
            throw new InvalidOperationException($"Handler '{handlerType}' does not expose HandleAsync.");
        }

        var task = method.Invoke(handler, [request, cancellationToken]) as Task<TResult>;
        if (task is null)
        {
            throw new InvalidOperationException($"Handler '{handlerType}' returned an invalid task.");
        }

        return await task.ConfigureAwait(false);
    }
}
