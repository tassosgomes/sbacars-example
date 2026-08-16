using System.Reflection;
using Rebus.Topic;
using SbaCars.Contracts;

namespace SbaCars.BuildingBlocks.Messaging.Topology;

/// <summary>
/// Names a Rebus topic after the published type's <see cref="IntegrationEventAttribute"/> (D4),
/// registered in place of Rebus' own <c>Rebus.Topic.DefaultTopicNameConvention</c> by
/// <c>MessagingServiceCollectionExtensions.AddSbaCarsMessaging</c> — see
/// <c>OptionsConfigurer.Register&lt;ITopicNameConvention&gt;</c> at that call site.
/// </summary>
/// <remarks>
/// Throwing when the attribute is missing is the intended behavior, not an oversight: Rebus'
/// <c>DefaultTopicNameConvention</c> falls back to the type's "short assembly-qualified name" when no
/// convention overrides it, which would make routing depend on a C# class name — silently, with no
/// error, and with the failure mode being "the wrong subscribers get the message" rather than
/// "publishing fails loudly". Requiring the attribute and failing fast when it is absent turns a
/// silent routing bug into a boot-time-adjacent, actionable exception instead.
/// </remarks>
public sealed class IntegrationEventTopicConvention : ITopicNameConvention
{
    public string GetTopic(Type eventType)
    {
        if (TryGetTopic(eventType, out var topic))
        {
            return topic;
        }

        throw new InvalidOperationException(
            $"Cannot publish '{eventType.FullName}': it has no [IntegrationEvent] attribute. Every " +
            "type published or subscribed to through this bus must be decorated with " +
            $"[IntegrationEvent(\"<business-name>\")] (see {nameof(SbaCars)}.{nameof(Contracts)}." +
            $"{nameof(IntegrationEventAttribute)}) so that its wire name is an explicit, reviewable " +
            "decision instead of an accident of what the .NET type happens to be called.");
    }

    /// <summary>
    /// Non-throwing lookup of the same wire name <see cref="GetTopic"/> would return, for callers
    /// that must not fail the operation just because a type turns out not to be a well-formed
    /// integration event — <c>CloudEventsOutgoingStep</c> (which omits <c>ce_type</c> rather than
    /// aborting the publish; the throwing behavior above is what already guards the publish itself)
    /// and <c>TracingOutgoingStep</c>/<c>TracingIncomingStep</c> (which fall back to a generic span
    /// name rather than losing the span). Internal: this is a reuse detail within
    /// <c>SbaCars.BuildingBlocks.Messaging</c>, not part of this project's public surface.
    /// </summary>
    internal static bool TryGetTopic(Type eventType, out string topic)
    {
        var attribute = eventType.GetCustomAttribute<IntegrationEventAttribute>(inherit: false);

        topic = attribute?.Name ?? string.Empty;
        return attribute is not null;
    }
}
