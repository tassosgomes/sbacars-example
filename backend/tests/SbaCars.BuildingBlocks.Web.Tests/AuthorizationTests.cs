using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SbaCars.BuildingBlocks.Web.Auth;

namespace SbaCars.BuildingBlocks.Web.Tests;

/// <summary>
/// Proves the authorization contract every service and gateway-backoffice gets from
/// <see cref="AuthExtensions.AddSbaCarsAuth"/>: no token is a 401 (default deny, §5.6), a token
/// missing the required permission is a 403, and a token carrying it is a 200. Signing and
/// validation both use an in-memory RSA key (<see cref="TestJwt"/>) — no real Logto involved, per
/// §9 of the architecture plan ("Autorização (volume)").
/// </summary>
public sealed class AuthorizationTests
{
    [Fact]
    public async Task NoToken_Returns401()
    {
        var (app, _) = await StartAuthHostAsync();
        await using var _ = app;
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/protected");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenWithoutTheRequiredPermission_Returns403()
    {
        var (app, signingKey) = await StartAuthHostAsync();
        await using var _ = app;
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.Create(signingKey, scope: "estoque:ler"));

        var response = await client.GetAsync("/protected");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TokenWithTheRequiredPermission_Returns200()
    {
        var (app, signingKey) = await StartAuthHostAsync();
        await using var _ = app;
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.Create(signingKey, scope: "estoque:gerenciar"));

        var response = await client.GetAsync("/protected");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TokenWithoutAScopeClaim_IsAuthenticatedButHoldsNoPermission_Returns403()
    {
        var (app, signingKey) = await StartAuthHostAsync();
        await using var _ = app;
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.Create(signingKey, scope: null));

        var response = await client.GetAsync("/protected");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<(WebApplication App, RsaSecurityKey SigningKey)> StartAuthHostAsync()
    {
        var signingKey = TestJwt.CreateSigningKey();

        var app = await TestHostFactory.StartAsync(
            configureBuilder: builder =>
            {
                builder.Services.AddSbaCarsAuth(builder.Configuration, builder.Environment);

                // Swaps Logto discovery (Authority/MetadataAddress) for the static test key —
                // runs after AddSbaCarsAuth's own Configure<JwtBearerOptions>, so it wins.
                builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = null;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters.ValidIssuer = TestJwt.Issuer;
                    options.TokenValidationParameters.IssuerSigningKey = signingKey;
                });
            },
            configureApp: webApp =>
            {
                webApp.UseSbaCarsAuth();
                webApp.MapGet("/protected", () => Results.Ok())
                    .RequireAuthorization(Permissoes.EstoqueGerenciar);
            });

        return (app, signingKey);
    }
}
