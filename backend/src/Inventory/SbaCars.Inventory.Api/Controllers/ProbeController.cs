using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Storage;
using SbaCars.BuildingBlocks.Web.Auth;
using SbaCars.Inventory.Application.Foundation;

namespace SbaCars.Inventory.Api.Controllers;

/// <summary>
/// A6 scaffolding — proves JWT authentication and permission-based authorization end to end
/// (default deny, 401/403/200) for this service. B5 adds <c>foundation-ping</c> to exercise the
/// messaging stack (§6.5). C3 adds storage probe endpoints. Remove probe endpoints once
/// inventory-service has real protected APIs.
/// </summary>
[ApiController]
[Route("api/_probe")]
public sealed class ProbeController(
    ICurrentUser currentUser,
    IFoundationPingProbeService foundationPingProbeService,
    IObjectStorage objectStorage,
    IOptions<StorageOptions> storageOptions) : ControllerBase
{
    [HttpGet("whoami")]
    [Authorize(Policy = Permissoes.EstoqueLer)]
    public IActionResult WhoAmI() =>
        Ok(new { userId = currentUser.UserId, permissions = currentUser.Permissions });

    /// <summary>
    /// B5 scaffolding (§6.5): publishes <c>foundation.ping</c> through the transactional outbox.
    /// Delete when the first real integration event is published from inventory.
    /// </summary>
    [HttpPost("foundation-ping")]
    [Authorize(Policy = Permissoes.EstoqueLer)]
    public async Task<IActionResult> FoundationPing(CancellationToken cancellationToken)
    {
        var pingId = await foundationPingProbeService.PublishPingAsync(cancellationToken);
        return Ok(new { pingId });
    }

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
