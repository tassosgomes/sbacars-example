using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.BuildingBlocks.Web.Auth;
using SbaCars.Inventory.Api.Contracts;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Ofertas.AlterarDisponibilidade;
using SbaCars.Inventory.Application.Ofertas.AtualizarVeiculo;
using SbaCars.Inventory.Application.Ofertas.CadastrarVeiculo;
using SbaCars.Inventory.Application.Ofertas.DefinirPrecoInicial;
using SbaCars.Inventory.Application.Ofertas.ExcluirOferta;
using SbaCars.Inventory.Application.Ofertas.ListarOfertas;
using SbaCars.Inventory.Application.Ofertas.ObterOferta;
using SbaCars.Inventory.Application.Ofertas.SubstituirFatos;
using SbaCars.Inventory.Application.Solicitacoes.AbrirSolicitacao;

namespace SbaCars.Inventory.Api.Controllers;

[ApiController]
[Route("api/ofertas")]
public sealed class OfertasController(IDispatcher dispatcher) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Permissoes.EstoqueGerenciar)]
    [ProducesResponseType(typeof(OfertaDetalheResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Cadastrar(
        [FromBody] CadastrarVeiculoRequest request,
        CancellationToken cancellationToken)
    {
        var response = await dispatcher
            .SendAsync(request.ToCommand(), cancellationToken)
            .ConfigureAwait(false);

        return Created($"/api/ofertas/{response.OfertaId}", response);
    }

    [HttpGet]
    [Authorize(Policy = Permissoes.EstoqueLer)]
    [ProducesResponseType(typeof(PagedResult<OfertaResumoResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OfertaResumoResponse>>> Listar(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PagedRequest.DefaultPageSize,
        [FromQuery] string? busca = null,
        [FromQuery] string[]? situacao = null,
        [FromQuery] string[]? disponibilidade = null,
        [FromQuery] string? uf = null,
        [FromQuery] string ordenarPor = "atualizadoEm:desc",
        CancellationToken cancellationToken = default)
    {
        var query = new ListarOfertasQuery
        {
            Page = page,
            PageSize = pageSize,
            Busca = busca,
            Situacao = situacao ?? [],
            Disponibilidade = disponibilidade ?? [],
            Uf = uf,
            OrdenarPor = ordenarPor,
        };

        var response = await dispatcher
            .QueryAsync(query, cancellationToken)
            .ConfigureAwait(false);
        return Ok(response);
    }

    [HttpGet("{ofertaId:guid}")]
    [Authorize(Policy = Permissoes.EstoqueLer)]
    [ProducesResponseType(typeof(OfertaDetalheResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OfertaDetalheResponse>> Obter(
        Guid ofertaId,
        CancellationToken cancellationToken)
    {
        var response = await dispatcher
            .QueryAsync(new ObterOfertaQuery(ofertaId), cancellationToken)
            .ConfigureAwait(false);

        return Ok(response);
    }

    [HttpPatch("{ofertaId:guid}/veiculo")]
    [Authorize(Policy = Permissoes.EstoqueGerenciar)]
    [ProducesResponseType(typeof(OfertaDetalheResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OfertaDetalheResponse>> AtualizarVeiculo(
        Guid ofertaId,
        [FromBody] AtualizarVeiculoRequest request,
        CancellationToken cancellationToken)
    {
        var response = await dispatcher
            .SendAsync(request.ToCommand(ofertaId), cancellationToken)
            .ConfigureAwait(false);

        return Ok(response);
    }

    [HttpPut("{ofertaId:guid}/preco")]
    [Authorize(Policy = Permissoes.EstoqueGerenciar)]
    [ProducesResponseType(typeof(OfertaDetalheResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OfertaDetalheResponse>> DefinirPreco(
        Guid ofertaId,
        [FromBody] DefinirPrecoInicialRequest request,
        CancellationToken cancellationToken)
    {
        var response = await dispatcher
            .SendAsync(request.ToCommand(ofertaId), cancellationToken)
            .ConfigureAwait(false);

        return Ok(response);
    }

    [HttpPut("{ofertaId:guid}/fatos")]
    [Authorize(Policy = Permissoes.EstoqueGerenciar)]
    [ProducesResponseType(typeof(OfertaDetalheResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OfertaDetalheResponse>> SubstituirFatos(
        Guid ofertaId,
        [FromBody] SubstituirFatosRequest request,
        CancellationToken cancellationToken)
    {
        var response = await dispatcher
            .SendAsync(request.ToCommand(ofertaId), cancellationToken)
            .ConfigureAwait(false);

        return Ok(response);
    }

    [HttpPost("{ofertaId:guid}/disponibilidade")]
    [Authorize(Policy = Permissoes.EstoqueGerenciar)]
    [ProducesResponseType(typeof(OfertaDetalheResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OfertaDetalheResponse>> AlterarDisponibilidade(
        Guid ofertaId,
        [FromBody] AlterarDisponibilidadeRequest request,
        CancellationToken cancellationToken)
    {
        var response = await dispatcher
            .SendAsync(request.ToCommand(ofertaId), cancellationToken)
            .ConfigureAwait(false);

        return Ok(response);
    }

    [HttpPost("{ofertaId:guid}/solicitacoes")]
    [Authorize(Policy = Permissoes.EstoqueGerenciar)]
    [ProducesResponseType(typeof(SolicitacaoDetalheResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SolicitacaoDetalheResponse>> AbrirSolicitacao(
        Guid ofertaId,
        [FromBody] AbrirSolicitacaoRequest request,
        CancellationToken cancellationToken)
    {
        var response = await dispatcher
            .SendAsync(request.ToCommand(ofertaId), cancellationToken)
            .ConfigureAwait(false);

        return Created($"/api/solicitacoes/{response.SolicitacaoId}", response);
    }

    [HttpDelete("{ofertaId:guid}")]
    [Authorize(Policy = Permissoes.EstoqueGerenciar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Excluir(
        Guid ofertaId,
        CancellationToken cancellationToken)
    {
        await dispatcher
            .SendAsync(new ExcluirOfertaCommand(ofertaId), cancellationToken)
            .ConfigureAwait(false);

        return NoContent();
    }
}
