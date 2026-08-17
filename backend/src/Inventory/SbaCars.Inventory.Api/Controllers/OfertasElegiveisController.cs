using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.BuildingBlocks.Web.Auth;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Integracao.ListarOfertasElegiveis;

namespace SbaCars.Inventory.Api.Controllers;

[ApiController]
[Route("api/ofertas-elegiveis")]
public sealed class OfertasElegiveisController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissoes.EstoqueIntegrar)]
    [ProducesResponseType(typeof(PagedResult<OfertaElegivelResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<OfertaElegivelResponse>>> Listar(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PagedRequest.DefaultPageSize,
        [FromQuery] DateTimeOffset? atualizadoApos = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ListarOfertasElegiveisQuery
        {
            Page = page,
            PageSize = pageSize,
            AtualizadoApos = atualizadoApos,
        };

        var response = await dispatcher
            .QueryAsync(query, cancellationToken)
            .ConfigureAwait(false);

        return Ok(response);
    }
}
