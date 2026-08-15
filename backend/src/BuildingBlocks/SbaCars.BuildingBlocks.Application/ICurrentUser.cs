namespace SbaCars.BuildingBlocks.Application;

/// <summary>
/// The authenticated caller, as seen by Application code. This is the only way a use case
/// learns who is calling and what they are allowed to do — never by reading
/// <c>HttpContext</c> or ASP.NET Core claims directly, which would leak a web dependency into
/// Application.
/// </summary>
/// <remarks>
/// Exposes permissions, never roles: per the project's authorization model, a use case asks
/// "can this caller do X?" (<see cref="HasPermission"/>) and never "is this caller in role Y?".
/// Roles remain a concept the identity provider owns; the claims transformation in
/// <c>BuildingBlocks.Web</c> is what turns a role's scopes into the permission strings exposed
/// here.
/// </remarks>
public interface ICurrentUser
{
    /// <summary>
    /// Whether the current request carries an authenticated identity.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// The subject identifier from the identity provider (the JWT <c>sub</c> claim). Treated as
    /// an opaque string, not a <c>Guid</c>: the identity provider does not guarantee that shape.
    /// <see langword="null"/> when <see cref="IsAuthenticated"/> is <see langword="false"/>.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// A display name for the current caller, when the token carries one.
    /// </summary>
    string? DisplayName { get; }

    /// <summary>
    /// The caller's permissions, each in the <c>recurso:acao</c> format (e.g.
    /// <c>"estoque:gerenciar"</c>). Never roles.
    /// </summary>
    IReadOnlyCollection<string> Permissions { get; }

    /// <summary>
    /// Whether the caller holds the given permission. Comparison is ordinal and
    /// case-insensitive: permission strings arrive from an external claim (ultimately an IdP
    /// scope), and a stray casing difference between how a policy names a permission and how
    /// the IdP was configured should not silently fail an authorization check.
    /// </summary>
    bool HasPermission(string permission);
}
