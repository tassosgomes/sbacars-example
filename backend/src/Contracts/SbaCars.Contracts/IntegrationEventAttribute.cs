namespace SbaCars.Contracts;

/// <summary>
/// Names an integration event on the wire (§6.3, §6.4 of the architecture plan): the string carried
/// by <see cref="Name"/> is what crosses the process boundary — it becomes the Rebus topic / routing
/// key and the CloudEvents <c>ce_type</c> header (see <c>IntegrationEventTopicConvention</c> and
/// <c>CloudEventsOutgoingStep</c> in <c>SbaCars.BuildingBlocks.Messaging</c>).
/// </summary>
/// <remarks>
/// This is the deliberate inverse of Rebus' own default: out of the box, Rebus names a topic after
/// the "short assembly-qualified type name" of the .NET type being published (see
/// <c>Rebus.Topic.DefaultTopicNameConvention</c>), which means renaming a C# class — an internal,
/// consumer-invisible change — would silently break every subscriber's binding. Requiring this
/// attribute instead makes the wire name an explicit, reviewable decision that can outlive whatever
/// the type is called in code, exactly as a domain event's business name (e.g.
/// <c>"estoque.oferta-incluida"</c>) is meant to.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IntegrationEventAttribute(string name) : Attribute
{
    /// <summary>
    /// The business name of the event as it crosses the wire — the Rebus topic/routing key and the
    /// CloudEvents <c>ce_type</c> value. Never the C# type name.
    /// </summary>
    public string Name { get; } = name;
}
