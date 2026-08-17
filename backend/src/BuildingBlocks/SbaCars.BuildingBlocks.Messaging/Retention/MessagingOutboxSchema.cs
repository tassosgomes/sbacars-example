namespace SbaCars.BuildingBlocks.Messaging.Retention;

/// <summary>
/// Holds the per-service PostgreSQL schema name for outbox/inbox retention when
/// <see cref="MessagingServiceCollectionExtensions.AddSbaCarsMessaging"/> is called with a schema.
/// </summary>
internal sealed class MessagingOutboxSchema(string value)
{
    public string Value { get; } = value;
}
