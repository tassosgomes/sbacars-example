using System.ComponentModel.DataAnnotations;

namespace SbaCars.BuildingBlocks.Storage;

/// <summary>
/// The <c>Storage</c> configuration section (§7 of the architecture plan): S3-compatible endpoint
/// and presigned-URL lifetimes. Bound through the Options Pattern with <c>ValidateOnStart</c> —
/// see <see cref="StorageOptionsValidator"/> for rules DataAnnotations cannot express.
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// S3 API endpoint (<c>ServiceURL</c> for <c>AmazonS3Config</c>) — MinIO in local compose,
    /// managed S3 in production. Must be an absolute <c>http://</c> or <c>https://</c> URI.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ServiceUrl { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string AccessKey { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// The single S3 bucket this service owns (§7). Each wired API configures exactly one bucket
    /// via <see cref="ObjectStorageBuckets"/>.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// AWS region for SigV4 signing. Required even when <see cref="ServiceUrl"/> points at MinIO.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Path-style addressing (<c>true</c> for MinIO; production S3 may set <c>false</c>).
    /// </summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>Presigned upload URL lifetime, in minutes.</summary>
    [Range(1, 1440)]
    public int UploadUrlLifetimeMinutes { get; set; } = 5;

    /// <summary>Presigned download URL lifetime, in minutes.</summary>
    [Range(1, 1440)]
    public int DownloadUrlLifetimeMinutes { get; set; } = 5;
}
