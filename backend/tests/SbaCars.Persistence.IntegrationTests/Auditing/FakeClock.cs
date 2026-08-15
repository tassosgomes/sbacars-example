using SbaCars.BuildingBlocks.Application;

namespace SbaCars.Persistence.IntegrationTests.Auditing;

/// <summary>
/// Fixed <see cref="IClock"/> test double, so an audit entry's <c>OccurredAt</c> can be asserted
/// against an exact value instead of "close to <see cref="DateTimeOffset.UtcNow"/>" — proving the
/// interceptor actually sources time from <see cref="IClock"/> and not <c>DateTime.UtcNow</c>.
/// </summary>
public sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);
}
