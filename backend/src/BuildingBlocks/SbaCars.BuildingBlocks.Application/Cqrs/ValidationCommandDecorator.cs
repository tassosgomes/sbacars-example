using FluentValidation;

namespace SbaCars.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Runs a command validator before the wrapped handler. The validator is optional so a command
/// without input rules still uses the same registration path; commands that have a validator are
/// never allowed to reach their handler when invalid.
/// </summary>
public sealed class ValidationCommandHandlerDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IValidator<TCommand>? validator) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (validator is not null)
        {
            var validationResult = await validator
                .ValidateAsync(command, cancellationToken)
                .ConfigureAwait(false);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }

        return await inner.HandleAsync(command, cancellationToken).ConfigureAwait(false);
    }
}
