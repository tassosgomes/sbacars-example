namespace SbaCars.Contracts.Foundation.V1;

/// <summary>
/// Technical B5 scaffolding event (§6.5): exercises outbox → broker → inbox without business rules.
/// Removed when the first real integration event is published in production.
/// </summary>
[IntegrationEvent("foundation.ping")]
public sealed record FoundationPingIntegrationEvent(Guid PingId, DateTimeOffset OcorridoEm) : IIntegrationEvent;
