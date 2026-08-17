using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Domain.Solicitacoes;

namespace SbaCars.Inventory.Application.Solicitacoes.ObterSolicitacao;

public sealed class ObterSolicitacaoHandler(
    ISolicitacaoRepository solicitacaoRepository,
    IOfertaRepository ofertaRepository,
    CalculadoraDiasUteis calculadora,
    ICurrentUser currentUser,
    IClock clock) : IQueryHandler<ObterSolicitacaoQuery, SolicitacaoDetalheResponse>
{
    public async Task<SolicitacaoDetalheResponse> HandleAsync(
        ObterSolicitacaoQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.SolicitacaoId == Guid.Empty)
        {
            throw new ArgumentException("solicitacaoId é obrigatório.", nameof(query));
        }

        var solicitacao = await solicitacaoRepository
            .ObterAsync(query.SolicitacaoId, cancellationToken)
            .ConfigureAwait(false);

        if (solicitacao is null)
        {
            throw new SolicitacaoNaoEncontradaException(query.SolicitacaoId);
        }

        var oferta = await ofertaRepository
            .ObterAsync(solicitacao.OfertaId, cancellationToken)
            .ConfigureAwait(false);

        if (oferta is null)
        {
            throw new OfertaNaoEncontradaException(solicitacao.OfertaId);
        }

        return SolicitacaoResponseMapper.ToDetalhe(
            solicitacao,
            oferta,
            calculadora,
            clock.UtcNow,
            currentUser.UserId);
    }
}
