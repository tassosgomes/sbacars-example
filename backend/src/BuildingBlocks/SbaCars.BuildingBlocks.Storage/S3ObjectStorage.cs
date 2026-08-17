using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using SbaCars.BuildingBlocks.Application;

namespace SbaCars.BuildingBlocks.Storage;

/// <summary>
/// <see cref="IObjectStorage"/> backed by <see cref="IAmazonS3"/> — same code path for AWS S3 and
/// MinIO via <see cref="StorageOptions.ServiceUrl"/> and <see cref="StorageOptions.ForcePathStyle"/>.
/// </summary>
internal sealed class S3ObjectStorage(IAmazonS3 s3, IOptions<StorageOptions> options) : IObjectStorage
{
    private readonly StorageOptions _options = options.Value;
    private readonly Protocol _protocol = ResolveProtocol(options.Value.ServiceUrl);

    public Task<ObjectStoragePresignedUrl> CreateUploadUrlAsync(
        string bucket,
        string key,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        cancellationToken.ThrowIfCancellationRequested();

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.UploadUrlLifetimeMinutes);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = expiresAt.UtcDateTime,
            ContentType = contentType,
            Protocol = _protocol,
        };

        return Task.FromResult(BuildPresignedUrl(request, expiresAt, contentType));
    }

    public Task<ObjectStoragePresignedUrl> CreateDownloadUrlAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.DownloadUrlLifetimeMinutes);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = expiresAt.UtcDateTime,
            Protocol = _protocol,
        };

        return Task.FromResult(BuildPresignedUrl(request, expiresAt, requiredContentType: null));
    }

    public async Task DeleteAsync(string bucket, string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();

        await s3.DeleteObjectAsync(bucket, key, cancellationToken).ConfigureAwait(false);
    }

    private ObjectStoragePresignedUrl BuildPresignedUrl(
        GetPreSignedUrlRequest request,
        DateTimeOffset expiresAt,
        string? requiredContentType)
    {
        var urlString = s3.GetPreSignedURL(request);
        IReadOnlyDictionary<string, string> requiredHeaders = requiredContentType is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string> { ["Content-Type"] = requiredContentType };

        return new ObjectStoragePresignedUrl(new Uri(urlString), requiredHeaders, expiresAt);
    }

    private static Protocol ResolveProtocol(string serviceUrl) =>
        Uri.TryCreate(serviceUrl, UriKind.Absolute, out var serviceUri) &&
        serviceUri.Scheme == Uri.UriSchemeHttps
            ? Protocol.HTTPS
            : Protocol.HTTP;
}
