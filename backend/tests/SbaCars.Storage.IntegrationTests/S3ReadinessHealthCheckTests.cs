using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SbaCars.BuildingBlocks.Storage;
using SbaCars.BuildingBlocks.Storage.HealthChecks;
using SbaCars.TestKit;

namespace SbaCars.Storage.IntegrationTests;

[Collection(SbaCarsMinioCollection.Name)]
public sealed class S3ReadinessHealthCheckTests
{
    private readonly SbaCarsMinioFixture _fixture;

    public S3ReadinessHealthCheckTests(SbaCarsMinioFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AgainstALiveMinio_TheCheckReportsHealthy()
    {
        var bucket = StorageTestConfiguration.UniqueBucketName("c3-head-bucket");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSbaCarsStorage(StorageTestConfiguration.Build(_fixture, bucket));
        services.AddHealthChecks().AddSbaCarsS3ReadinessCheck("test");

        await using var provider = services.BuildServiceProvider();
        var s3 = provider.GetRequiredService<IAmazonS3>();
        await StorageTestConfiguration.EnsureBucketExistsAsync(s3, bucket);

        var healthCheckService = provider.GetRequiredService<HealthCheckService>();
        var report = await healthCheckService.CheckHealthAsync();

        report.Status.Should().Be(HealthStatus.Healthy);
        report.Entries.Should().ContainKey("s3")
            .WhoseValue.Status.Should().Be(HealthStatus.Healthy);
    }
}
