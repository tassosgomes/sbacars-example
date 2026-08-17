using Testcontainers.Minio;
using Xunit;

namespace SbaCars.TestKit;

/// <summary>
/// One <c>minio/minio:RELEASE.2025-07-23T15-54-02Z</c> container for storage integration tests
/// (§7, C1). The same image tag should be used in <c>docker-compose.yml</c> when C2 adds MinIO
/// to the local stack.
/// </summary>
public sealed class SbaCarsMinioFixture : IAsyncLifetime
{
    public const string ImageTag = "minio/minio:RELEASE.2025-07-23T15-54-02Z";

    private const string DefaultAccessKey = "minioadmin";
    private const string DefaultSecretKey = "minioadmin";

    public MinioContainer Container { get; }

    public SbaCarsMinioFixture()
    {
        Container = new MinioBuilder(ImageTag)
            .WithUsername(DefaultAccessKey)
            .WithPassword(DefaultSecretKey)
            .Build();
    }

    public Task InitializeAsync() => Container.StartAsync();

    public Task DisposeAsync() => Container.DisposeAsync().AsTask();

    /// <summary>Path-style S3 API base URL (<c>http://host:port</c>) for this container.</summary>
    public string ServiceUrl => Container.GetConnectionString();

    public string AccessKey => DefaultAccessKey;

    public string SecretKey => DefaultSecretKey;
}
