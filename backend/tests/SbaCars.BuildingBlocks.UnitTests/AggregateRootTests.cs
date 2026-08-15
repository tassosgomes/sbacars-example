using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.BuildingBlocks.UnitTests;

public class AggregateRootTests
{
    private sealed record SampleEvent(string Payload) : IDomainEvent;

    private sealed class SampleAggregate : AggregateRoot
    {
        public void DoSomething(string payload) => RaiseDomainEvent(new SampleEvent(payload));
    }

    [Fact]
    public void DomainEvents_WhenAggregateIsNew_IsEmpty()
    {
        // Arrange & Act
        var aggregate = new SampleAggregate();

        // Assert
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RaiseDomainEvent_AddsEventToPendingCollection()
    {
        // Arrange
        var aggregate = new SampleAggregate();

        // Act
        aggregate.DoSomething("first");
        aggregate.DoSomething("second");

        // Assert
        aggregate.DomainEvents.Should().HaveCount(2);
        aggregate.DomainEvents.Should().ContainInOrder(
            new SampleEvent("first"),
            new SampleEvent("second"));
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllPendingEvents()
    {
        // Arrange
        var aggregate = new SampleAggregate();
        aggregate.DoSomething("first");

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DomainEvents_CannotBeMutatedFromOutside()
    {
        // Arrange
        var aggregate = new SampleAggregate();
        aggregate.DoSomething("first");

        // Act
        var exposed = (ICollection<IDomainEvent>)aggregate.DomainEvents;
        var act = () => exposed.Add(new SampleEvent("injected"));

        // Assert
        act.Should().Throw<NotSupportedException>();
        aggregate.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void DomainEvents_ReflectsAggregateStateAtReadTime()
    {
        // Arrange
        var aggregate = new SampleAggregate();
        var snapshot = aggregate.DomainEvents;

        // Act
        aggregate.DoSomething("first");

        // Assert: the wrapper returned earlier tracks the same backing list.
        snapshot.Should().HaveCount(1);
    }
}
