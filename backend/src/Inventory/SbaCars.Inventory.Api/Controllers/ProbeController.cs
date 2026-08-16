using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Web.Auth;
using SbaCars.Inventory.Application.Foundation;

namespace SbaCars.Inventory.Api.Controllers;

/// <summary>
/// A6 scaffolding — proves JWT authentication and permission-based authorization end to end
/// (default deny, 401/403/200) for this service. B5 adds <c>foundation-ping</c> to exercise the
/// messaging stack (§6.5). Remove probe endpoints once inventory-service has real protected APIs.
/// </summary>
[ApiController]
[Route("api/_probe")]
public sealed class ProbeController(
    ICurrentUser currentUser,
    IFoundationPingProbeService foundationPingProbeService) : ControllerBase
{
    [HttpGet("whoami")]
    [Authorize(Policy = Permissoes.EstoqueLer)]
    public IActionResult WhoAmI() =>
        Ok(new { userId = currentUser.UserId, permissions = currentUser.Permissions });

    /// <summary>
    /// B5 scaffolding (§6.5): publishes <c>foundation.ping</c> through the transactional outbox.
    /// Delete when the first real integration event is published from inventory.
    /// </summary>
    [HttpPost("foundation-ping")]
    [Authorize(Policy = Permissoes.EstoqueLer)]
    public async Task<IActionResult> FoundationPing(CancellationToken cancellationToken)
    {
        var pingId = await foundationPingProbeService.PublishPingAsync(cancellationToken);
        return Ok(new { pingId });
    }
}
