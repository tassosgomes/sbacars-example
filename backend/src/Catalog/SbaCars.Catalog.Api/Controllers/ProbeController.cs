using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Storage;
using SbaCars.BuildingBlocks.Web.Auth;

namespace SbaCars.Catalog.Api.Controllers;

/// <summary>
/// A6 scaffolding — proves JWT authentication and permission-based authorization end to end
/// (default deny, 401/403/200) for this service. C3 adds storage probe endpoints. No business
/// rule here. Remove once catalog-service has real protected endpoints.
/// </summary>
[ApiController]
[Route("api/_probe")]
public sealed class ProbeController(
    ICurrentUser currentUser,
    IObjectStorage objectStorage,
    IOptions<StorageOptions> storageOptions) : ControllerBase
{
    [HttpGet("whoami")]
    [Authorize(Policy = Permissoes.CatalogoGerenciar)]
    public IActionResult WhoAmI() =>
        Ok(new { userId = currentUser.UserId, permissions = currentUser.Permissions });

    /// <summary>
    /// A7 scaffolding — proves that a request through gateway-public's anonymous
    /// <c>/api/catalog/{**rest}</c> route reaches this service with the path rewritten to
    /// <c>/api/_probe/ping</c>. No business rule here. Remove once catalog-service has a real
    /// anonymous read endpoint (D01).
    /// </summary>
    [HttpGet("ping")]
    [AllowAnonymous]
    public IActionResult Ping() => Ok(new { service = "catalog", status = "ok" });

    /// <summary>
    /// C3 scaffolding (§7): presigned upload URL for the catalog media bucket. Delete when D01
    /// catalog media endpoints exist.
    /// </summary>
    [HttpPost("storage/upload-url")]
    [Authorize(Policy = Permissoes.CatalogoGerenciar)]
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
    /// C3 scaffolding (§7): presigned download URL for probe objects only. Delete when D01
    /// catalog media endpoints exist.
    /// </summary>
    [HttpPost("storage/download-url")]
    [Authorize(Policy = Permissoes.CatalogoGerenciar)]
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
