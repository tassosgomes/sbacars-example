using Microsoft.AspNetCore.Authorization;

namespace SbaCars.BuildingBlocks.Web.Auth;

/// <summary>
/// Succeeds a <see cref="PermissionRequirement"/> when the principal carries a matching
/// <c>permission</c> claim (written by <see cref="ScopeClaimsTransformation"/>). Comparison is
/// ordinal, case-insensitive — the same rule <c>ICurrentUser.HasPermission</c> uses (A2), kept
/// consistent so a policy and a use case never disagree about the same token.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var hasPermission = context.User.Claims.Any(claim =>
            claim.Type == ScopeClaimsTransformation.PermissionClaimType &&
            string.Equals(claim.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
