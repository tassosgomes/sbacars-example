using System.Text;
using System.Text.Json;
using Rebus.Messages;
using Rebus.Serialization;

namespace SbaCars.BuildingBlocks.Messaging.Sagas;

/// <summary>
/// Minimal <see cref="ISerializer"/> for saga timeout deferral that matches Rebus' default
/// System.Text.Json wire format.
/// </summary>
internal sealed class SystemTextJsonRebusSerializer : ISerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public Task<TransportMessage> Serialize(Message message)
    {
        var headers = new Dictionary<string, string>(message.Headers);
        var bodyType = message.Body.GetType();
        headers.TryAdd(Headers.MessageId, Guid.NewGuid().ToString());
        headers.TryAdd(Headers.Intent, Headers.IntentOptions.PointToPoint);
        headers.TryAdd(Headers.Type, bodyType.AssemblyQualifiedName!);
        headers.TryAdd(Headers.ContentType, "application/json; charset=utf-8");

        var json = JsonSerializer.Serialize(message.Body, bodyType, SerializerOptions);
        var transportMessage = new TransportMessage(headers, Encoding.UTF8.GetBytes(json));
        return Task.FromResult(transportMessage);
    }

    public Task<Message> Deserialize(TransportMessage transportMessage) =>
        throw new NotSupportedException("Saga timeout deferral only serializes outgoing messages.");
}
