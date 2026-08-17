using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace SbaCars.Storage.IntegrationTests;

internal static class StorageTestConfiguration
{
    public static IConfiguration Build(SbaCars.TestKit.SbaCarsMinioFixture fixture, string bucketName = "test-bucket") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:ServiceUrl"] = fixture.ServiceUrl,
                ["Storage:AccessKey"] = fixture.AccessKey,
                ["Storage:SecretKey"] = fixture.SecretKey,
                ["Storage:ForcePathStyle"] = "true",
                ["Storage:Region"] = "us-east-1",
                ["Storage:BucketName"] = bucketName,
            })
            .Build();

    public static string UniqueBucketName(string prefix) =>
        $"{prefix}-{Guid.NewGuid():N}".ToLowerInvariant();

    public static async Task EnsureBucketExistsAsync(IAmazonS3 s3, string bucketName)
    {
        try
        {
            await s3.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode is "BucketAlreadyOwnedByYou")
        {
        }
    }
}
