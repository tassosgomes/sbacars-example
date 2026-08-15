namespace SbaCars.BuildingBlocks.Domain;

/// <summary>
/// Base class for objects with a stable identity. Equality is decided exclusively by
/// <see cref="Id"/> and the runtime type — never by the object's other state.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    /// <summary>
    /// The entity identity. New entities receive a time-ordered <c>Guid</c> v7
    /// (<see cref="Guid.CreateVersion7()"/>) unless an explicit id is supplied, which is the
    /// path used when an entity is rebuilt from a persisted or external value.
    /// </summary>
    public Guid Id { get; protected init; }

    protected Entity()
        : this(Guid.CreateVersion7())
    {
    }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Entity id cannot be empty.", nameof(id));
        }

        Id = id;
    }

    public bool Equals(Entity? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return GetType() == other.GetType() && Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as Entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}
