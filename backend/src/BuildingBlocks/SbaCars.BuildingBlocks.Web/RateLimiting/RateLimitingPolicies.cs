namespace SbaCars.BuildingBlocks.Web.RateLimiting;

/// <summary>
/// Names of the rate limiting policies registered by
/// <see cref="RateLimitingExtensions.AddSbaCarsRateLimiting"/>. Routes that need the stricter
/// anonymous-surface limit (e.g. the manifestação de interesse endpoint, once it exists) opt in
/// with <c>.RequireRateLimiting(RateLimitingPolicies.AnonymousStrict)</c>; every other request
/// is already covered by the general policy applied globally.
/// </summary>
public static class RateLimitingPolicies
{
    /// <summary>The stricter, per-IP sliding window meant for anonymous write endpoints.</summary>
    public const string AnonymousStrict = "sbacars-anonymous-strict";
}
