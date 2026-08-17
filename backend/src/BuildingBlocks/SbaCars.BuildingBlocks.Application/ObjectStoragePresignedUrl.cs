namespace SbaCars.BuildingBlocks.Application;

/// <summary>
/// A presigned object-storage URL and the headers the client must send (upload only for PUT).
/// </summary>
/// <param name="Url">The presigned URL the client calls directly — binary never transits the API.</param>
/// <param name="RequiredHeaders">
/// Headers that must accompany the request (e.g. signed <c>Content-Type</c> on upload). May be empty
/// for download.
/// </param>
/// <param name="ExpiresAt">When the URL stops being valid.</param>
public sealed record ObjectStoragePresignedUrl(
    Uri Url,
    IReadOnlyDictionary<string, string> RequiredHeaders,
    DateTimeOffset ExpiresAt);
