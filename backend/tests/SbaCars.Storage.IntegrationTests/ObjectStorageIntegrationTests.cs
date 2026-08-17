using System.Net;
using System.Net.Http.Headers;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.DependencyInjection;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Storage;
using SbaCars.TestKit;

namespace SbaCars.Storage.IntegrationTests;

/// <summary>
/// C1 proof: presigned upload/download, anonymous access denied, delete, all against real MinIO.
/// </summary>
[Collection(SbaCarsMinioCollection.Name)]
public sealed class ObjectStorageIntegrationTests
{
    private readonly SbaCarsMinioFixture _fixture;

    public ObjectStorageIntegrationTests(SbaCarsMinioFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PresignedUploadThenDownload_ReturnsTheSameBytes()
    {
        var bucket = StorageTestConfiguration.UniqueBucketName("c1-upload-download");
        var key = $"objects/{Guid.NewGuid():N}.bin";
        var payload = "sbacars-c1-storage-proof"u8.ToArray();
        const string contentType = "application/octet-stream";

        using var provider = BuildProvider();
        var storage = provider.GetRequiredService<IObjectStorage>();
        var s3 = provider.GetRequiredService<IAmazonS3>();

        await s3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });

        var upload = await storage.CreateUploadUrlAsync(bucket, key, contentType);
        using var http = new HttpClient();
        using var putContent = new ByteArrayContent(payload);
        putContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        foreach (var (headerName, headerValue) in upload.RequiredHeaders)
        {
            putContent.Headers.Remove(headerName);
            putContent.Headers.TryAddWithoutValidation(headerName, headerValue);
        }

        var putResponse = await http.PutAsync(upload.Url, putContent);
        putResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        var download = await storage.CreateDownloadUrlAsync(bucket, key);
        var downloaded = await http.GetByteArrayAsync(download.Url);

        downloaded.Should().Equal(payload);
    }

    [Fact]
    public async Task AnonymousGetWithoutSignature_IsDenied()
    {
        var bucket = StorageTestConfiguration.UniqueBucketName("c1-anonymous");
        var key = $"objects/{Guid.NewGuid():N}.bin";
        var payload = "private-object"u8.ToArray();
        const string contentType = "text/plain";

        using var provider = BuildProvider();
        var storage = provider.GetRequiredService<IObjectStorage>();
        var s3 = provider.GetRequiredService<IAmazonS3>();

        await s3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });

        var upload = await storage.CreateUploadUrlAsync(bucket, key, contentType);
        using var http = new HttpClient();
        using var putContent = new ByteArrayContent(payload);
        putContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        (await http.PutAsync(upload.Url, putContent)).IsSuccessStatusCode.Should().BeTrue();

        var anonymousUrl = new Uri(new Uri(_fixture.ServiceUrl), $"{bucket}/{key}");
        var anonymousResponse = await http.GetAsync(anonymousUrl);

        anonymousResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheObject()
    {
        var bucket = StorageTestConfiguration.UniqueBucketName("c1-delete");
        var key = $"objects/{Guid.NewGuid():N}.bin";
        var payload = "to-be-deleted"u8.ToArray();
        const string contentType = "application/octet-stream";

        using var provider = BuildProvider();
        var storage = provider.GetRequiredService<IObjectStorage>();
        var s3 = provider.GetRequiredService<IAmazonS3>();

        await s3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });

        var upload = await storage.CreateUploadUrlAsync(bucket, key, contentType);
        using var http = new HttpClient();
        using var putContent = new ByteArrayContent(payload);
        putContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        (await http.PutAsync(upload.Url, putContent)).IsSuccessStatusCode.Should().BeTrue();

        await storage.DeleteAsync(bucket, key);

        var act = () => s3.GetObjectAsync(new GetObjectRequest { BucketName = bucket, Key = key });
        await act.Should().ThrowAsync<AmazonS3Exception>();
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSbaCarsStorage(StorageTestConfiguration.Build(_fixture));
        return services.BuildServiceProvider();
    }
}
