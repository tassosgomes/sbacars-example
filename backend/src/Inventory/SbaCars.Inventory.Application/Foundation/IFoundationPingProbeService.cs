namespace SbaCars.Inventory.Application.Foundation;

/// <summary>
/// B5 scaffolding (§6.5): publishes <c>foundation.ping</c> through the transactional outbox.
/// Delete with the probe endpoint when the first real integration event exists.
/// </summary>
public interface IFoundationPingProbeService
{
    Task<Guid> PublishPingAsync(CancellationToken cancellationToken = default);
}
