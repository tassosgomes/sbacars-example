namespace SbaCars.Catalog.Api.Messaging.Foundation;

/// <summary>
/// B5 scaffolding (§6.5): observable handler effect for <c>foundation.ping</c> — inbox proves
/// durability; this proves the handler ran. Delete when the first real catalog consumer exists.
/// </summary>
public sealed class FoundationPingReceipt
{
    private int _handleCount;

    public int HandleCount => _handleCount;

    public Guid? LastPingId { get; private set; }

    public string? LastMessageId { get; private set; }

    public string? ObservedTraceparent { get; private set; }

    public void Record(Guid pingId, string? messageId, string? traceparent)
    {
        LastPingId = pingId;
        LastMessageId = messageId;
        ObservedTraceparent = traceparent;
        Interlocked.Increment(ref _handleCount);
    }

    public async Task<bool> WaitForHandleCountAsync(int expected, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (Volatile.Read(ref _handleCount) >= expected)
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        return false;
    }
}
