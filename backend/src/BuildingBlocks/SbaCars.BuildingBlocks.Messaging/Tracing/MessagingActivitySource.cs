using System.Diagnostics;

namespace SbaCars.BuildingBlocks.Messaging.Tracing;

/// <summary>
/// The single <see cref="ActivitySource"/> both <see cref="TracingOutgoingStep"/> and
/// <see cref="TracingIncomingStep"/> start spans on (§6.3.2, D8). <see cref="Name"/> is public and
/// <see langword="const"/> because it is needed in two independent places that must agree on the
/// exact same string: the <c>AddSource(MessagingActivitySource.Name)</c> call
/// <c>MessagingServiceCollectionExtensions.AddSbaCarsMessaging</c> makes against the host's
/// <c>TracerProviderBuilder</c> (otherwise spans are created but never sampled/exported), and any
/// test that attaches an in-memory exporter to observe them.
/// </summary>
public static class MessagingActivitySource
{
    public const string Name = "SbaCars.Messaging";

    public static readonly ActivitySource Instance = new(Name);
}
