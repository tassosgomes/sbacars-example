using System.Security.Claims;
using SbaCars.BuildingBlocks.Web.Auth;

namespace SbaCars.BuildingBlocks.Web.Tests;

/// <summary>
/// Unit-level coverage of §5.6's normalization step: a space-separated <c>scope</c> claim becomes
/// exactly one <c>permission</c> claim per scope, nothing more and nothing less.
/// </summary>
public sealed class ScopeClaimsTransformationTests
{
    private readonly ScopeClaimsTransformation _sut = new();

    [Fact]
    public async Task ScopeWithMultipleValues_ResolvesExactlyThosePermissionsAndNoOthers()
    {
        var principal = AuthenticatedPrincipal(new Claim("scope", "estoque:ler catalogo:gerenciar"));

        var result = await _sut.TransformAsync(principal);

        result.FindAll(ScopeClaimsTransformation.PermissionClaimType)
            .Select(claim => claim.Value)
            .Should().BeEquivalentTo(["estoque:ler", "catalogo:gerenciar"]);
    }

    [Fact]
    public async Task NoScopeClaim_ResolvesToNoPermissionsWithoutThrowing()
    {
        var principal = AuthenticatedPrincipal(new Claim("sub", "user-1"));

        var result = await _sut.TransformAsync(principal);

        result.FindAll(ScopeClaimsTransformation.PermissionClaimType).Should().BeEmpty();
    }

    [Fact]
    public async Task BlankScopeClaim_ResolvesToNoPermissions()
    {
        var principal = AuthenticatedPrincipal(new Claim("scope", "   "));

        var result = await _sut.TransformAsync(principal);

        result.FindAll(ScopeClaimsTransformation.PermissionClaimType).Should().BeEmpty();
    }

    [Fact]
    public async Task UnauthenticatedPrincipal_IsReturnedUntouched()
    {
        // No authenticationType => ClaimsIdentity.IsAuthenticated is false.
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("scope", "estoque:ler")]));

        var result = await _sut.TransformAsync(principal);

        result.Should().BeSameAs(principal);
        result.FindAll(ScopeClaimsTransformation.PermissionClaimType).Should().BeEmpty();
    }

    [Fact]
    public async Task AppliedTwice_DoesNotDuplicatePermissionClaims()
    {
        var principal = AuthenticatedPrincipal(new Claim("scope", "estoque:ler"));

        await _sut.TransformAsync(principal);
        var result = await _sut.TransformAsync(principal);

        result.FindAll(ScopeClaimsTransformation.PermissionClaimType).Should().ContainSingle();
    }

    private static ClaimsPrincipal AuthenticatedPrincipal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "TestAuth"));
}
