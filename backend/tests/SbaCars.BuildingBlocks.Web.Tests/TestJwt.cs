using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SbaCars.BuildingBlocks.Web.Auth;

namespace SbaCars.BuildingBlocks.Web.Tests;

/// <summary>
/// A minimal, in-process stand-in for the "own key and in-memory JWKS" test fixture §9 of the
/// architecture plan calls for: an RSA key pair generated once and used both to sign test tokens
/// and, statically, as the JwtBearer scheme's <c>IssuerSigningKey</c> in
/// <c>AuthorizationTests</c> — no HTTP call, no real Logto, no discovery document. A full
/// <c>SbaCars.TestKit</c> project (shared across service test suites) is A9's job; this project
/// only needs enough to exercise <c>BuildingBlocks.Web</c> itself.
/// </summary>
internal static class TestJwt
{
    /// <summary>An arbitrary issuer string — never dereferenced over the network in these tests.</summary>
    public const string Issuer = "https://test-issuer.example/oidc";

    public static RsaSecurityKey CreateSigningKey() => new(RSA.Create(2048));

    public static string Create(
        RsaSecurityKey signingKey,
        string subject = "test-user",
        string? scope = null,
        string audience = AuthExtensions.Audience)
    {
        var claims = new Dictionary<string, object> { ["sub"] = subject };
        if (scope is not null)
        {
            claims["scope"] = scope;
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            Claims = claims,
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
