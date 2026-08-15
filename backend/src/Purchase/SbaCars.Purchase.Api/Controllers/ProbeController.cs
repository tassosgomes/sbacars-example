using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SbaCars.BuildingBlocks.Application;

namespace SbaCars.Purchase.Api.Controllers;

/// <summary>
/// A6 scaffolding — proves JWT authentication end to end (default deny, 401/200) for this
/// service. purchase-service has no Phase 1 permission of its own yet (D04 is Fase 2, §5.4), so
/// this only requires an authenticated caller rather than a specific policy. No business rule
/// here. Remove once purchase-service has a real protected endpoint.
/// </summary>
[ApiController]
[Route("api/_probe")]
public sealed class ProbeController(ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("whoami")]
    [Authorize]
    public IActionResult WhoAmI() =>
        Ok(new { userId = currentUser.UserId, permissions = currentUser.Permissions });
}
