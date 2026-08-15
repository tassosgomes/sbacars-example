using SbaCars.BuildingBlocks.Application;

namespace SbaCars.BuildingBlocks.UnitTests;

public class SystemClockTests
{
    [Fact]
    public void UtcNow_ReturnsUtcOffset()
    {
        // Arrange
        IClock clock = new SystemClock();

        // Act
        var now = clock.UtcNow;

        // Assert
        now.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void UtcNow_ReflectsTheCurrentInstant()
    {
        // Arrange
        IClock clock = new SystemClock();
        var before = DateTimeOffset.UtcNow;

        // Act
        var now = clock.UtcNow;
        var after = DateTimeOffset.UtcNow;

        // Assert
        now.Should().BeOnOrAfter(before);
        now.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void UtcNow_CalledTwice_Advances()
    {
        // Arrange
        IClock clock = new SystemClock();

        // Act
        var first = clock.UtcNow;
        Thread.Sleep(5);
        var second = clock.UtcNow;

        // Assert
        second.Should().BeAfter(first);
    }
}
