using FluentValidation;

namespace SbaCars.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Validates a query before invoking its read handler. Queries do not mutate state, but validating
/// their filters here keeps malformed requests from reaching a repository and producing a
/// provider-specific error.
/// </summary>
public sealed class ValidationQueryHandlerDecorator<TQuery, TResult>(
    IQueryHandler<TQuery, TResult> inner,
    IValidator<TQuery>? validator) : IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (validator is not null)
        {
            var validationResult = await validator
                .ValidateAsync(query, cancellationToken)
                .ConfigureAwait(false);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }

        return await inner.HandleAsync(query, cancellationToken).ConfigureAwait(false);
    }
}
