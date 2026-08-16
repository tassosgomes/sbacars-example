namespace SbaCars.BuildingBlocks.Application;

/// <summary>
/// Publishes an integration event to the message bus (§6 of the architecture plan). Lives here, next
/// to <see cref="IUnitOfWork"/>, following the exact same split this codebase already uses for every
/// other infrastructure concern a use case depends on: the port is a pure interface in Application,
/// and the concrete adapter (<c>RebusIntegrationEventPublisher</c>, backed by Rebus) lives in
/// <c>SbaCars.BuildingBlocks.Messaging</c> — Application itself never references Rebus.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why <see cref="object"/> and not a generic constrained to some <c>IIntegrationEvent</c>.</b>
/// The marker interface and the attribute that names an event on the wire both live in
/// <c>SbaCars.Contracts</c> (§3.3 — Contracts is dependency-free by construction, referenced by
/// every service), but <c>BuildingBlocks.Application</c> does not reference <c>Contracts</c>: nothing
/// else in Application needs it, and adding the reference here only to constrain this one signature
/// would pull a dependency in for a single method. Accepting <see cref="object"/> keeps that
/// boundary intact. The real validation of "is this actually a well-formed integration event" does
/// not belong here anyway — it happens in <c>IntegrationEventTopicConvention</c>, in <c>.Messaging</c>,
/// which is where a missing/malformed event contract fails with an actionable message before
/// anything is put on the wire.
/// </para>
/// <para>
/// <b>Why this is not registered in Application.</b> Like every other port in this file's
/// neighborhood, only the interface lives here — the DI registration of its implementation happens
/// where the implementation lives (<c>AddSbaCarsMessaging</c>, in <c>.Messaging</c>), never here.
/// </para>
/// </remarks>
public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Publishes <paramref name="integrationEvent"/> to every subscriber of its wire topic (derived
    /// from its <c>[IntegrationEvent]</c> attribute — see <c>IntegrationEventTopicConvention</c>).
    /// </summary>
    /// <remarks>
    /// In B2, this same interface starts being called from inside the outbox's
    /// <c>RebusTransactionScope</c> instead of directly — no use case that calls
    /// <see cref="PublishAsync"/> today needs to change when that happens; only the implementation
    /// registered behind this interface does.
    /// </remarks>
    Task PublishAsync(object integrationEvent, CancellationToken cancellationToken = default);
}
