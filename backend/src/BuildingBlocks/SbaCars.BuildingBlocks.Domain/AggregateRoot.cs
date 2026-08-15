namespace SbaCars.BuildingBlocks.Domain;

/// <summary>
/// Base class for an aggregate's entry point. Adds nothing to <see cref="Entity"/> beyond the
/// bookkeeping of pending domain events raised while the aggregate is mutated in memory.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot()
    {
    }

    protected AggregateRoot(Guid id)
        : base(id)
    {
    }

    /// <summary>
    /// Domain events raised by this aggregate that have not yet been dispatched. Read-only by
    /// construction: callers cannot add to or remove from the underlying list through this
    /// property.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Registers a domain event as pending. Called from within the aggregate's own behavior,
    /// never from outside it.
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Drops all pending domain events. Called once they have been dispatched (e.g. after the
    /// outbox has recorded them).
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
