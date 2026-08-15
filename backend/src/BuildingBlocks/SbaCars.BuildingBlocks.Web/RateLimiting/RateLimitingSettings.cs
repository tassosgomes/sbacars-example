using System.ComponentModel.DataAnnotations;

namespace SbaCars.BuildingBlocks.Web.RateLimiting;

/// <summary>
/// The <c>RateLimiting</c> configuration section: a per-IP sliding window that applies to every
/// request reaching <c>gateway-public</c>, plus a stricter window applied on top for the
/// anonymous surface called out in §5.5 of the architecture plan (e.g. manifestação de
/// interesse). All bounds ship with sane defaults so the section is optional, but any value that
/// is present must be sane too — validated with <c>ValidateOnStart</c>.
/// </summary>
public sealed class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    /// <summary>Requests allowed per <see cref="WindowSeconds"/> for the general policy, per IP.</summary>
    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; } = 100;

    /// <summary>Width, in seconds, of the general sliding window.</summary>
    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;

    /// <summary>Number of segments the general sliding window is divided into.</summary>
    [Range(1, 60)]
    public int SegmentsPerWindow { get; set; } = 4;

    /// <summary>
    /// Requests allowed to queue, per IP, once the general limit is reached, before being
    /// rejected outright. Zero means reject immediately.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int QueueLimit { get; set; }

    /// <summary>Requests allowed per <see cref="AnonymousStrictWindowSeconds"/> for the stricter, anonymous-surface policy, per IP.</summary>
    [Range(1, int.MaxValue)]
    public int AnonymousStrictPermitLimit { get; set; } = 10;

    /// <summary>Width, in seconds, of the stricter sliding window.</summary>
    [Range(1, 3600)]
    public int AnonymousStrictWindowSeconds { get; set; } = 60;
}
