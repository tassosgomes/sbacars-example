namespace SbaCars.BuildingBlocks.Messaging.CloudEvents;

/// <summary>
/// Transport-message header names for the CloudEvents 1.0 "binary content mode" envelope (§6.3.2 of
/// the architecture plan) — the mode where CloudEvents attributes travel as protocol headers and the
/// message body stays exactly what the sender serialized, rather than being wrapped in a CloudEvents
/// JSON envelope. Stamped by <see cref="CloudEventsOutgoingStep"/>.
/// </summary>
public static class CloudEventHeaders
{
    public const string SpecVersion = "ce_specversion";
    public const string Id = "ce_id";
    public const string Type = "ce_type";
    public const string Source = "ce_source";
    public const string Time = "ce_time";
    public const string DataContentType = "ce_datacontenttype";
}
