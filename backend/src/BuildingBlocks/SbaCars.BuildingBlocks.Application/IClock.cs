namespace SbaCars.BuildingBlocks.Application;

/// <summary>
/// The single source of "now" for a service. Application and Domain code must depend on this
/// instead of calling <c>DateTime.Now</c>/<c>DateTime.UtcNow</c>/<c>DateTimeOffset.UtcNow</c>
/// directly, so that time is deterministic and replaceable in tests.
/// </summary>
public interface IClock
{
    /// <summary>
    /// The current instant, always in UTC.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}

/// <summary>
/// Production implementation of <see cref="IClock"/>, backed by the system clock.
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
