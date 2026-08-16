using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Web.Auth;

namespace SbaCars.Catalog.Api.Controllers;

/// <summary>
/// A6 scaffolding — proves JWT authentication and permission-based authorization end to end
/// (default deny, 401/403/200) for this service. No business rule here. Remove once
/// catalog-service has a real protected endpoint.
/// </summary>
[ApiController]
[Route("api/_probe")]
public sealed class ProbeController(ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("whoami")]
    [Authorize(Policy = Permissoes.CatalogoGerenciar)]
    public IActionResult WhoAmI() =>
        Ok(new { userId = currentUser.UserId, permissions = currentUser.Permissions });

    /// <summary>
    /// A7 scaffolding — proves that a request through gateway-public's anonymous
    /// <c>/api/catalog/{**rest}</c> route reaches this service with the path rewritten to
    /// <c>/api/_probe/ping</c>. No business rule here. Remove once catalog-service has a real
    /// anonymous read endpoint (D01).
    /// </summary>
    [HttpGet("ping")]
    [AllowAnonymous]
    public IActionResult Ping() => Ok(new { service = "catalog", status = "ok" });
}
