using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Web.Auth;

namespace SbaCars.Interest.Api.Controllers;

/// <summary>
/// A6 scaffolding — proves JWT authentication and permission-based authorization end to end
/// (default deny, 401/403/200) for this service. No business rule here. Remove once
/// interest-service has a real protected endpoint.
/// </summary>
[ApiController]
[Route("api/_probe")]
public sealed class ProbeController(ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("whoami")]
    [Authorize(Policy = Permissoes.AtendimentoGerenciar)]
    public IActionResult WhoAmI() =>
        Ok(new { userId = currentUser.UserId, permissions = currentUser.Permissions });

    /// <summary>
    /// A7 scaffolding — proves that a request through gateway-public's anonymous
    /// <c>/api/interest/{**rest}</c> route reaches this service with the path rewritten to
    /// <c>/api/_probe/ping</c>. No business rule here. Remove once interest-service has a real
    /// anonymous write endpoint (D03's manifestação de interesse).
    /// </summary>
    /// <remarks>
    /// <c>POST</c>, not <c>GET</c>, unlike catalog's: the public edge only routes
    /// <c>POST</c>/<c>OPTIONS</c> to this service, because the anonymous surface it will
    /// eventually expose is a write (§5.5). A <c>GET</c> probe here would be unreachable through
    /// the very route it claims to prove, and would exercise neither the method restriction nor
    /// the stricter <c>sbacars-anonymous-strict</c> rate-limit policy that route carries.
    /// </remarks>
    [HttpPost("ping")]
    [AllowAnonymous]
    public IActionResult Ping() => Ok(new { service = "interest", status = "ok" });
}
