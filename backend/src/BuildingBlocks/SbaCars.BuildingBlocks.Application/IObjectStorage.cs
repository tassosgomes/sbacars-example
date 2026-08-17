namespace SbaCars.BuildingBlocks.Application;

/// <summary>
/// Object storage port (§7 of the architecture plan): presigned upload and download URLs plus delete.
/// Lives here, next to <see cref="IIntegrationEventPublisher"/>, so Application never references
/// <c>AWSSDK.S3</c> — the concrete S3/MinIO adapter lives in
/// <c>SbaCars.BuildingBlocks.Storage</c>.
/// </summary>
public interface IObjectStorage
{
    /// <summary>
    /// Creates a short-lived presigned HTTP PUT URL. The client must upload with the exact
    /// <c>Content-Type</c> returned in <see cref="ObjectStoragePresignedUrl.RequiredHeaders"/>.
    /// </summary>
    Task<ObjectStoragePresignedUrl> CreateUploadUrlAsync(
        string bucket,
        string key,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a short-lived presigned HTTP GET URL for downloading an existing object.
    /// </summary>
    Task<ObjectStoragePresignedUrl> CreateDownloadUrlAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the object at <paramref name="key"/> in <paramref name="bucket"/>.
    /// </summary>
    Task DeleteAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default);
}
