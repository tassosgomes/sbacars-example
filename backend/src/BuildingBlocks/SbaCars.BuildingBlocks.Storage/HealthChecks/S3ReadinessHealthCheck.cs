using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace SbaCars.BuildingBlocks.Storage.HealthChecks;

/// <summary>
/// The S3/MinIO leg of <c>/health/ready</c> (§8): can this process reach its configured bucket.
/// Uses <see cref="AmazonS3Client.HeadBucketAsync"/> on <see cref="StorageOptions.BucketName"/>
/// — least-privilege IAM in production will not grant <c>s3:ListAllMyBuckets</c>.
/// </summary>
public sealed class S3ReadinessHealthCheck(IAmazonS3 s3, IOptions<StorageOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var bucketName = options.Value.BucketName;

        try
        {
            await s3.HeadBucketAsync(new HeadBucketRequest { BucketName = bucketName }, cancellationToken)
                .ConfigureAwait(false);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"S3 bucket '{bucketName}' is not reachable.", ex);
        }
    }
}
