using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.BuildingBlocks.Web.Auth;
using SbaCars.Inventory.Api.Contracts;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Solicitacoes.ContarPendentes;
using SbaCars.Inventory.Application.Solicitacoes.ListarFilaValidacao;
using SbaCars.Inventory.Application.Solicitacoes.ObterSolicitacao;

namespace SbaCars.Inventory.Api.Controllers;

[ApiController]
[Route("api/solicitacoes")]
public sealed class SolicitacoesController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissoes.EstoqueValidar)]
    [ProducesResponseType(typeof(PagedResult<SolicitacaoResumoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<SolicitacaoResumoResponse>>> Listar(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PagedRequest.DefaultPageSize,
        [FromQuery] string? status = "pendente",
        [FromQuery] string[]? tipo = null,
        [FromQuery] string ordenarPor = "abertaEm:asc",
        CancellationToken cancellationToken = default)
    {
        var query = new ListarFilaValidacaoQuery
        {
            Page = page,
            PageSize = pageSize,
            Status = status,
            Tipo = tipo ?? [],
            OrdenarPor = ordenarPor,
        };

        var response = await dispatcher
            .QueryAsync(query, cancellationToken)
            .ConfigureAwait(false);
        return Ok(response);
    }

    [HttpGet("pendentes/contagem")]
    [Authorize(Policy = Permissoes.EstoqueValidar)]
    [ProducesResponseType(typeof(ContagemPendentesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ContagemPendentesResponse>> Contar(
        CancellationToken cancellationToken)
    {
        var response = await dispatcher
            .QueryAsync(new ContarPendentesQuery(), cancellationToken)
            .ConfigureAwait(false);
        return Ok(response);
    }

    [HttpGet("{solicitacaoId:guid}")]
    [Authorize(Policy = Permissoes.EstoqueValidar)]
    [ProducesResponseType(typeof(SolicitacaoDetalheResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SolicitacaoDetalheResponse>> Obter(
        Guid solicitacaoId,
        CancellationToken cancellationToken)
    {
        var response = await dispatcher
            .QueryAsync(new ObterSolicitacaoQuery(solicitacaoId), cancellationToken)
            .ConfigureAwait(false);
        return Ok(response);
    }

    [HttpPost("{solicitacaoId:guid}/aprovar")]
    [Authorize(Policy = Permissoes.EstoqueValidar)]
    [ProducesResponseType(typeof(SolicitacaoDetalheResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SolicitacaoDetalheResponse>> Aprovar(
        Guid solicitacaoId,
        [FromBody] AprovarSolicitacaoRequest? request,
        CancellationToken cancellationToken)
    {
        var command = (request ?? new AprovarSolicitacaoRequest()).ToCommand(solicitacaoId);
        var response = await dispatcher
            .SendAsync(command, cancellationToken)
            .ConfigureAwait(false);
        return Ok(response);
    }

    [HttpPost("{solicitacaoId:guid}/rejeitar")]
    [Authorize(Policy = Permissoes.EstoqueValidar)]
    [ProducesResponseType(typeof(SolicitacaoDetalheResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SolicitacaoDetalheResponse>> Rejeitar(
        Guid solicitacaoId,
        [FromBody] RejeitarSolicitacaoRequest? request,
        CancellationToken cancellationToken)
    {
        var command = (request ?? new RejeitarSolicitacaoRequest()).ToCommand(solicitacaoId);
        var response = await dispatcher
            .SendAsync(command, cancellationToken)
            .ConfigureAwait(false);
        return Ok(response);
    }
}
