namespace SbaCars.BuildingBlocks.Web.ErrorHandling;

/// <summary>
/// Explicit, extensible registry of exception type to HTTP status/title. Seeded by
/// <see cref="ProblemDetailsExtensions.AddSbaCarsProblemDetails"/> with the one mapping every
/// service shares (<see cref="SbaCars.BuildingBlocks.Domain.DomainException"/>); each service adds
/// its own concrete exceptions later, through the <c>configureExceptions</c> callback of that same
/// method, without ever touching this file.
/// </summary>
/// <remarks>
/// Lookup walks the exception's type hierarchy from the concrete runtime type up to (but
/// excluding) <see cref="object"/>, so a service can register a very specific exception type and
/// still fall back to a coarser mapping (e.g. the shared <c>DomainException</c> entry) for any
/// subtype it did not register explicitly.
/// </remarks>
public sealed class ExceptionProblemDetailsMap
{
    private readonly Dictionary<Type, (int StatusCode, string Title)> _mappings = [];

    /// <summary>
    /// Registers (or replaces) the status code and title used when the handled exception is, or
    /// derives from, <typeparamref name="TException"/>.
    /// </summary>
    public ExceptionProblemDetailsMap Map<TException>(int statusCode, string title)
        where TException : Exception
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        _mappings[typeof(TException)] = (statusCode, title);
        return this;
    }

    /// <summary>
    /// Resolves the most specific registered mapping for <paramref name="exception"/>, if any.
    /// </summary>
    public bool TryResolve(Exception exception, out int statusCode, out string title)
    {
        ArgumentNullException.ThrowIfNull(exception);

        for (var type = exception.GetType(); type is not null && type != typeof(object); type = type.BaseType)
        {
            if (_mappings.TryGetValue(type, out var mapping))
            {
                statusCode = mapping.StatusCode;
                title = mapping.Title;
                return true;
            }
        }

        statusCode = 0;
        title = string.Empty;
        return false;
    }
}
