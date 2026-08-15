using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Web.Auth;

namespace SbaCars.Inventory.Api.Controllers;

/// <summary>
/// A6 scaffolding — proves JWT authentication and permission-based authorization end to end
/// (default deny, 401/403/200) for this service. No business rule here. Remove once
/// inventory-service has a real protected endpoint.
/// </summary>
[ApiController]
[Route("api/_probe")]
public sealed class ProbeController(ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("whoami")]
    [Authorize(Policy = Permissoes.EstoqueLer)]
    public IActionResult WhoAmI() =>
        Ok(new { userId = currentUser.UserId, permissions = currentUser.Permissions });
}
