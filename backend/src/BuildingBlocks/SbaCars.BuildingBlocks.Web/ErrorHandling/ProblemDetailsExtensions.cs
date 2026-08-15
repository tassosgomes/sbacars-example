using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.BuildingBlocks.Web.ErrorHandling;

/// <summary>
/// Wires the global <see cref="GlobalExceptionHandler"/> and the RFC 9457 ProblemDetails
/// machinery it depends on. Every host in the solution calls this once.
/// </summary>
public static class ProblemDetailsExtensions
{
    /// <summary>
    /// Registers ASP.NET Core's built-in <see cref="IProblemDetailsService"/>, the
    /// <see cref="GlobalExceptionHandler"/>, and the exception-to-status mapping it uses.
    /// </summary>
    /// <param name="configureExceptions">
    /// Lets a service register its own concrete exceptions on top of the one mapping every
    /// service shares (<see cref="DomainException"/> → 422 Unprocessable Entity). This is the
    /// seam mentioned in the architecture plan: each service extends the map from its own
    /// <c>Program.cs</c>, and <c>BuildingBlocks.Web</c> never needs to know about a concrete,
    /// service-specific exception type.
    /// </param>
    public static IServiceCollection AddSbaCarsProblemDetails(
        this IServiceCollection services,
        Action<ExceptionProblemDetailsMap>? configureExceptions = null)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        var exceptionMap = new ExceptionProblemDetailsMap()
            .Map<DomainException>(StatusCodes.Status422UnprocessableEntity, "Business rule violation");
        configureExceptions?.Invoke(exceptionMap);

        services.AddSingleton(exceptionMap);

        return services;
    }
}
