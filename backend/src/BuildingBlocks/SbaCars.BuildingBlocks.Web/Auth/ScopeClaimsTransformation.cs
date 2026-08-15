using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace SbaCars.BuildingBlocks.Web.Auth;

/// <summary>
/// Projects the Logto <c>scope</c> claim (a space-separated OAuth scope string, per RFC 6749
/// §3.3) into one <c>permission</c> claim per scope. This is the only place in the system that
/// reads a claim shaped by the identity provider — every policy, <see cref="CurrentUser"/> and
/// piece of Application code downstream speaks only the business vocabulary of a permission
/// (§5.6 of the architecture plan). Registered as <see cref="IClaimsTransformation"/>, which the
/// framework's <c>AuthenticationService</c> runs automatically after a successful
/// authentication — no explicit call site needed.
/// </summary>
public sealed class ScopeClaimsTransformation : IClaimsTransformation
{
    /// <summary>The claim type this transformation writes, and the one policies check.</summary>
    public const string PermissionClaimType = "permission";

    private const string ScopeClaimType = "scope";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } identity)
        {
            return Task.FromResult(principal);
        }

        // AuthenticationService can invoke a registered IClaimsTransformation more than once for
        // the same request. Guard against duplicating claims on a second pass rather than relying
        // on every caller to transform exactly once.
        if (identity.HasClaim(claim => claim.Type == PermissionClaimType))
        {
            return Task.FromResult(principal);
        }

        var scope = identity.FindFirst(ScopeClaimType)?.Value;
        if (string.IsNullOrWhiteSpace(scope))
        {
            // No `scope` claim at all — e.g. a token issued without the sbacars API resource —
            // means zero permissions, not an error. ICurrentUser.Permissions comes back empty and
            // every permission-gated policy below denies with 403: default deny (§5.6), one layer
            // under FallbackPolicy's 401.
            return Task.FromResult(principal);
        }

        foreach (var permission in scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            identity.AddClaim(new Claim(PermissionClaimType, permission));
        }

        return Task.FromResult(principal);
    }
}
