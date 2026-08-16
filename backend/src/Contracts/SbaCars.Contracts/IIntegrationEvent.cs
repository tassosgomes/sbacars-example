namespace SbaCars.Contracts;

/// <summary>
/// Marker interface for the <c>record</c> types under <c>SbaCars.Contracts</c> that represent an
/// integration event published on <c>sbacars.events</c> (§6 of the architecture plan). Carries no
/// members on purpose — the wire contract of an event is its <see cref="IntegrationEventAttribute"/>
/// name plus its own properties, not a shape imposed here. The concrete <c>record</c>s (Domain Docs'
/// events, e.g. <c>OfertaIncluidaEvent</c>) are B4 work; this interface exists now so B1's plumbing
/// (topic convention, publisher abstraction) has something to reference without waiting for them.
/// </summary>
public interface IIntegrationEvent;
