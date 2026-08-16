using SbaCars.BuildingBlocks.Application;
using SbaCars.Contracts.Foundation.V1;

namespace SbaCars.Inventory.Application.Foundation;

public sealed class FoundationPingProbeService(
    IIntegrationEventPublisher publisher,
    IUnitOfWork unitOfWork) : IFoundationPingProbeService
{
    public async Task<Guid> PublishPingAsync(CancellationToken cancellationToken = default)
    {
        var pingId = Guid.NewGuid();
        await publisher.PublishAsync(
            new FoundationPingIntegrationEvent(pingId, DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return pingId;
    }
}
