namespace SbaCars.Contracts;

/// <summary>
/// Marker interface for the <c>record</c> types under <c>SbaCars.Contracts</c> that represent an
/// integration event published on <c>sbacars.events</c> (§6 of the architecture plan). Carries no
/// members on purpose — the wire contract of an event is its <see cref="IntegrationEventAttribute"/>
/// name plus its own properties, not a shape imposed here. The concrete <c>record</c>s under
/// <c>SbaCars.Contracts.*.V1</c> (Domain Docs' integration events, e.g.
/// <c>OfertaIncluidaIntegrationEvent</c>) implement this marker; their shape is guarded by the
/// committed <c>schema-snapshot.json</c> (§9, B4).
/// </summary>
public interface IIntegrationEvent;
