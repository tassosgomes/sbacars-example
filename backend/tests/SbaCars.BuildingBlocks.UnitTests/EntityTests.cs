using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.BuildingBlocks.UnitTests;

public class EntityTests
{
    private sealed class SampleEntityA : Entity
    {
        public SampleEntityA()
        {
        }

        public SampleEntityA(Guid id)
            : base(id)
        {
        }
    }

    private sealed class SampleEntityB : Entity
    {
        public SampleEntityB(Guid id)
            : base(id)
        {
        }
    }

    [Fact]
    public void Constructor_WithoutExplicitId_GeneratesTimeOrderedGuid()
    {
        // Arrange & Act
        var entity = new SampleEntityA();

        // Assert
        entity.Id.Should().NotBe(Guid.Empty);
        entity.Id.Version.Should().Be(7);
    }

    [Fact]
    public void Constructor_WithEmptyId_Throws()
    {
        // Arrange & Act
        var act = () => new SampleEntityA(Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equals_WithSameIdAndSameType_ReturnsTrue()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        var first = new SampleEntityA(id);
        var second = new SampleEntityA(id);

        // Act & Assert
        first.Equals(second).Should().BeTrue();
        first.Should().Be(second);
    }

    [Fact]
    public void Equals_WithSameIdButDifferentType_ReturnsFalse()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        var a = new SampleEntityA(id);
        var b = new SampleEntityB(id);

        // Act & Assert
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentId_ReturnsFalse()
    {
        // Arrange
        var first = new SampleEntityA(Guid.CreateVersion7());
        var second = new SampleEntityA(Guid.CreateVersion7());

        // Act & Assert
        first.Equals(second).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var entity = new SampleEntityA(Guid.CreateVersion7());

        // Act & Assert
        entity.Equals(null).Should().BeFalse();
        (entity == null).Should().BeFalse();
        (null == entity).Should().BeFalse();
    }

    [Fact]
    public void EqualityOperator_WithTwoNulls_ReturnsTrue()
    {
        // Arrange
        SampleEntityA? left = null;
        SampleEntityA? right = null;

        // Act & Assert
        (left == right).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperators_MirrorEquals()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        var first = new SampleEntityA(id);
        var second = new SampleEntityA(id);
        var third = new SampleEntityA(Guid.CreateVersion7());

        // Act & Assert
        (first == second).Should().BeTrue();
        (first != third).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_ForEqualEntities_IsTheSame()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        var first = new SampleEntityA(id);
        var second = new SampleEntityA(id);

        // Act & Assert
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void GetHashCode_ForDifferentTypesWithSameId_Differs()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        var a = new SampleEntityA(id);
        var b = new SampleEntityB(id);

        // Act & Assert
        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    [Fact]
    public void Equals_WithSameReference_ReturnsTrue()
    {
        // Arrange
        var entity = new SampleEntityA(Guid.CreateVersion7());

        // Act & Assert
        entity.Equals(entity).Should().BeTrue();
    }
}
