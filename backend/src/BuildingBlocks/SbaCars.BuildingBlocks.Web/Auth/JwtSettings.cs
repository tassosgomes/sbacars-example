using System.ComponentModel.DataAnnotations;

namespace SbaCars.BuildingBlocks.Web.Auth;

/// <summary>
/// The <c>Jwt</c> configuration section (§5.2 of the architecture plan). <see cref="Authority"/>
/// is deliberately the only field meant to vary between local, dev and production — audience,
/// lifetime, signature and clock-skew rules are identical everywhere and live in code, in
/// <see cref="AuthExtensions"/>, not in configuration.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// The token issuer, validated against the token's <c>iss</c> claim through the OIDC
    /// discovery document. Defaults to Logto's host-facing endpoint (§5.1), which is what a
    /// service running outside a container reaches directly — the path that works with no
    /// further configuration.
    /// </summary>
    [Required]
    public string Authority { get; set; } = "http://localhost:3001/oidc";

    /// <summary>
    /// Overrides only where the discovery document (and, through it, the JWKS) is fetched from —
    /// never which issuer is validated, which keeps coming from the document's own <c>issuer</c>
    /// field regardless of which address served it. Set this for a service running in a
    /// container that cannot resolve <see cref="Authority"/>'s <c>localhost</c>, pointing it
    /// instead at Logto's address on the container network (e.g.
    /// <c>http://logto:3001/oidc/.well-known/openid-configuration</c> — the "armadilha de rede"
    /// of §5.1). Left <see langword="null"/> — the default — for a service running outside a
    /// container, which reaches <see cref="Authority"/> directly.
    /// </summary>
    public string? MetadataAddress { get; set; }
}
