using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SbaCars.BuildingBlocks.Application;

namespace SbaCars.BuildingBlocks.Web.Auth;

/// <summary>
/// The concrete, web-specific implementation of <c>ICurrentUser</c> (defined in
/// <c>BuildingBlocks.Application</c>, with zero ASP.NET Core dependency). This is the only class
/// in the system allowed to read <see cref="HttpContext"/> for identity — Application code
/// depends on the interface, never on this type or on <see cref="HttpContext"/> directly.
/// </summary>
internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public string? UserId => IsAuthenticated
        ? httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
            ?? httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        : null;

    public string? DisplayName =>
        httpContextAccessor.HttpContext?.User.FindFirst("name")?.Value
            ?? httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value;

    public IReadOnlyCollection<string> Permissions =>
        httpContextAccessor.HttpContext?.User
            .FindAll(ScopeClaimsTransformation.PermissionClaimType)
            .Select(claim => claim.Value)
            .ToArray()
        ?? [];

    public bool HasPermission(string permission) =>
        Permissions.Any(p => string.Equals(p, permission, StringComparison.OrdinalIgnoreCase));
}
