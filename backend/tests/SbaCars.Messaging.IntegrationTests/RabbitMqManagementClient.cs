using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SbaCars.Messaging.IntegrationTests;

/// <summary>
/// Thin wrapper over the RabbitMQ management HTTP API (exposed by the <c>-management</c> image
/// variant on port 15672), used by <see cref="TopologyDeclarationTests"/>,
/// <see cref="ConnectionBudgetTests"/> and <see cref="RetryAndErrorQueueTests"/> to verify topology,
/// bindings and live connections against the real broker — never against this process' in-memory
/// <c>MessagingOptions</c>. Verifying against the broker is the whole point of those tests:
/// configuration that never reaches the broker is exactly the failure they exist to catch.
/// </summary>
internal sealed class RabbitMqManagementClient : IDisposable
{
    // "/" URL-encoded, per the management API's own convention for addressing the default vhost.
    private const string DefaultVirtualHostSegment = "%2F";

    private readonly HttpClient _httpClient;

    public RabbitMqManagementClient(Uri baseUri, string username, string password)
    {
        _httpClient = new HttpClient { BaseAddress = baseUri };

        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    public Task<JsonElement> GetExchangesAsync(CancellationToken cancellationToken) =>
        GetJsonArrayAsync($"/api/exchanges/{DefaultVirtualHostSegment}", cancellationToken);

    public Task<JsonElement> GetQueuesAsync(CancellationToken cancellationToken) =>
        GetJsonArrayAsync($"/api/queues/{DefaultVirtualHostSegment}", cancellationToken);

    public Task<JsonElement> GetConnectionsAsync(CancellationToken cancellationToken) =>
        GetJsonArrayAsync("/api/connections", cancellationToken);

    private async Task<JsonElement> GetJsonArrayAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.Clone();
    }

    public void Dispose() => _httpClient.Dispose();
}
