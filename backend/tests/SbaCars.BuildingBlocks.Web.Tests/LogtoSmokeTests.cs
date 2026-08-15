using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SbaCars.BuildingBlocks.Web.Auth;

namespace SbaCars.BuildingBlocks.Web.Tests;

/// <summary>
/// One smoke test against the real Logto instance the A6 task describes as already running
/// locally (<c>http://localhost:3001</c>), proving <see cref="AuthExtensions.AddSbaCarsAuth"/>'s
/// real discovery/JWKS path — live <c>Authority</c>, issuer taken from the live discovery
/// document, real signature verification against Logto's actual signing key — works end to end,
/// not just against the in-memory key <see cref="AuthorizationTests"/> uses.
/// </summary>
/// <remarks>
/// <para>
/// <b>Opt-in, not part of the default run.</b> It needs a live Logto reachable at
/// <c>http://localhost:3001</c> and the Management API M2M credentials from
/// <c>infra/logto/.env</c> (the same ones <c>infra/logto/bootstrap.mjs</c> uses) to mint a real
/// token — neither is available on a clean checkout or in CI. Gated on
/// <c>SBACARS_LOGTO_SMOKE=1</c> (plus <c>LOGTO_M2M_APP_ID</c>/<c>LOGTO_M2M_APP_SECRET</c> in the
/// environment) so <c>dotnet test</c> stays green and deterministic everywhere else — the same
/// reasoning §9 of the architecture plan gives for keeping the real-Logto fixture separate from
/// the fast in-memory suite.
/// </para>
/// <para>
/// <b>What this does and does not prove.</b> The token minted here is a machine-to-machine
/// (<c>client_credentials</c>) token for the M2M app <c>bootstrap.mjs</c> already uses — it comes
/// back with the right <c>iss</c>/<c>aud</c> but with <i>no</i> <c>scope</c> claim, because that
/// M2M app was never granted an <c>https://api.sbacars.app</c> scope. That is exactly the "token
/// without a scope claim" case <see cref="ScopeClaimsTransformation"/> must handle, so this proves
/// authentication (real JWKS, real signature) end to end and that permission-less-but-authenticated
/// is denied by a permission policy. It does <b>not</b> exercise a real user token (ana/bruno)
/// carrying <c>estoque:*</c>/<c>catalogo:*</c>/<c>atendimento:*</c> scopes — that only comes out of
/// the Authorization Code + PKCE browser login the backoffice SPA performs, which cannot be
/// scripted headlessly (Logto's discovery document does not advertise a password grant).
/// </para>
/// </remarks>
public sealed class LogtoSmokeTests
{
    [Fact]
    public async Task RealLogtoIssuedToken_AuthenticatesButIsDeniedAPermissionItWasNotGranted()
    {
        if (Environment.GetEnvironmentVariable("SBACARS_LOGTO_SMOKE") != "1")
        {
            return; // Opt-in only — see remarks. Not a failure: infrastructure is simply absent.
        }

        var clientId = Environment.GetEnvironmentVariable("LOGTO_M2M_APP_ID")
            ?? throw new InvalidOperationException("LOGTO_M2M_APP_ID must be set (see infra/logto/.env).");
        var clientSecret = Environment.GetEnvironmentVariable("LOGTO_M2M_APP_SECRET")
            ?? throw new InvalidOperationException("LOGTO_M2M_APP_SECRET must be set (see infra/logto/.env).");

        using var tokenClient = new HttpClient { BaseAddress = new Uri("http://localhost:3001") };
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "/oidc/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["resource"] = AuthExtensions.Audience,
            }),
        };
        var tokenResponse = await tokenClient.SendAsync(tokenRequest);
        tokenResponse.EnsureSuccessStatusCode();
        var payload = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = payload.GetProperty("access_token").GetString();

        await using var app = await TestHostFactory.StartAsync(
            configuration: new Dictionary<string, string?> { ["Jwt:Authority"] = "http://localhost:3001/oidc" },
            configureBuilder: builder => builder.Services.AddSbaCarsAuth(builder.Configuration, builder.Environment),
            configureApp: webApp =>
            {
                webApp.UseSbaCarsAuth();
                webApp.MapGet("/authenticated-only", () => Results.Ok()).RequireAuthorization();
                webApp.MapGet("/needs-permission", () => Results.Ok())
                    .RequireAuthorization(Permissoes.EstoqueGerenciar);
            });
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var authenticatedOnly = await client.GetAsync("/authenticated-only");
        var needsPermission = await client.GetAsync("/needs-permission");

        authenticatedOnly.StatusCode.Should().Be(HttpStatusCode.OK);
        needsPermission.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
