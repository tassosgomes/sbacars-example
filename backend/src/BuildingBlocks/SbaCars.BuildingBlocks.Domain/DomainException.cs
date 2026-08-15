namespace SbaCars.BuildingBlocks.Domain;

/// <summary>
/// Root of every business-rule failure raised by a Domain project. Application and Api layers
/// can catch this type to tell an expected business failure apart from an unexpected one,
/// without depending on any concrete exception from a specific service.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
