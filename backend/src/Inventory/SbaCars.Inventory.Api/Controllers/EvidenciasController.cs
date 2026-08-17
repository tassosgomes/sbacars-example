using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.BuildingBlocks.Web.Auth;
using SbaCars.Inventory.Api.Contracts;
using SbaCars.Inventory.Application.Evidencias;
using SbaCars.Inventory.Application.Evidencias.GerarUrlDownload;
using SbaCars.Inventory.Application.Evidencias.GerarUrlUpload;

namespace SbaCars.Inventory.Api.Controllers;

[ApiController]
public sealed class EvidenciasController(IDispatcher dispatcher) : ControllerBase
{
    [HttpPost("api/ofertas/{ofertaId:guid}/evidencias/upload-url")]
    [Authorize(Policy = Permissoes.EstoqueGerenciar)]
    [ProducesResponseType(typeof(UploadEvidenciaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> GerarUrlUpload(
        Guid ofertaId,
        [FromBody] GerarUrlUploadEvidenciaRequest request,
        CancellationToken cancellationToken)
    {
        var response = await dispatcher
            .SendAsync(request.ToCommand(ofertaId), cancellationToken)
            .ConfigureAwait(false);

        return Created($"/api/evidencias/{response.EvidenciaId}/download-url", response);
    }

    [HttpGet("api/evidencias/{evidenciaId:guid}/download-url")]
    [Authorize(Policy = Permissoes.EstoqueLer)]
    [ProducesResponseType(typeof(DownloadEvidenciaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DownloadEvidenciaResponse>> GerarUrlDownload(
        Guid evidenciaId,
        CancellationToken cancellationToken)
    {
        var response = await dispatcher
            .QueryAsync(new GerarUrlDownloadQuery(evidenciaId), cancellationToken)
            .ConfigureAwait(false);

        return Ok(response);
    }
}
