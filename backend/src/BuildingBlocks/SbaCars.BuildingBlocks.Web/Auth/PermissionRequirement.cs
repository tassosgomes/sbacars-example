using Microsoft.AspNetCore.Authorization;

namespace SbaCars.BuildingBlocks.Web.Auth;

/// <summary>
/// "The caller holds permission X." One instance backs each permission-named policy registered
/// by <see cref="AuthExtensions.AddSbaCarsAuth"/> — never a role.
/// </summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
