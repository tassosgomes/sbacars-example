namespace SbaCars.BuildingBlocks.Domain;

/// <summary>
/// Marker for a domain event: something that already happened inside an aggregate and that
/// other parts of the system may care about. Carries no behavior of its own — concrete events
/// live in each service's Domain project.
/// </summary>
public interface IDomainEvent
{
}
