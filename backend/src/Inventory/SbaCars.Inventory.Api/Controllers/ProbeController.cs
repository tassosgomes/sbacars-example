using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Storage;
using SbaCars.BuildingBlocks.Web.Auth;

namespace SbaCars.Inventory.Api.Controllers;

/// <summary>
/// C3 storage probes. Remove these endpoints when D02/V-11 provides the real evidence endpoints.
/// </summary>
[ApiController]
[Route("api/_probe")]
public sealed class ProbeController(
    IObjectStorage objectStorage,
    IOptions<StorageOptions> storageOptions) : ControllerBase
{
    /// <summary>
    /// C3 scaffolding (§7): presigned upload URL for the inventory docs bucket. Delete when D02
    /// evidence endpoints exist.
    /// </summary>
    [HttpPost("storage/upload-url")]
    [Authorize(Policy = Permissoes.EstoqueGerenciar)]
    public async Task<IActionResult> CreateUploadUrl(
        [FromBody] StorageUploadUrlRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ContentType))
        {
            return BadRequest();
        }

        var bucket = storageOptions.Value.BucketName;
        var key = CreateProbeKey(request.FileName);
        var presigned = await objectStorage.CreateUploadUrlAsync(bucket, key, request.ContentType, cancellationToken);

        return Ok(ToResponse(bucket, key, presigned));
    }

    /// <summary>
    /// C3 scaffolding (§7): presigned download URL for probe objects only. Delete when D02
    /// evidence endpoints exist.
    /// </summary>
    [HttpPost("storage/download-url")]
    [Authorize(Policy = Permissoes.EstoqueLer)]
    public async Task<IActionResult> CreateDownloadUrl(
        [FromBody] StorageDownloadUrlRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsAllowedProbeKey(request.Key))
        {
            return BadRequest();
        }

        var bucket = storageOptions.Value.BucketName;
        var presigned = await objectStorage.CreateDownloadUrlAsync(bucket, request.Key, cancellationToken);

        return Ok(ToResponse(bucket, request.Key, presigned));
    }

    private static string CreateProbeKey(string? fileName)
    {
        var extension = string.Empty;
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var candidate = Path.GetExtension(fileName);
            if (!string.IsNullOrEmpty(candidate) &&
                candidate.All(static c => char.IsLetterOrDigit(c) || c == '.'))
            {
                extension = candidate.ToLowerInvariant();
            }
        }

        return $"probes/{Guid.NewGuid():N}{extension}";
    }

    private static bool IsAllowedProbeKey(string? key) =>
        !string.IsNullOrWhiteSpace(key) &&
        key.StartsWith("probes/", StringComparison.Ordinal) &&
        !key.Contains("..", StringComparison.Ordinal);

    private static StoragePresignedUrlResponse ToResponse(
        string bucket,
        string key,
        ObjectStoragePresignedUrl presigned) =>
        new(bucket, key, presigned.Url, presigned.RequiredHeaders, presigned.ExpiresAt);

    public sealed record StorageUploadUrlRequest(string? ContentType, string? FileName);

    public sealed record StorageDownloadUrlRequest(string Key);

    public sealed record StoragePresignedUrlResponse(
        string Bucket,
        string Key,
        Uri Url,
        IReadOnlyDictionary<string, string> RequiredHeaders,
        DateTimeOffset ExpiresAt);
}
