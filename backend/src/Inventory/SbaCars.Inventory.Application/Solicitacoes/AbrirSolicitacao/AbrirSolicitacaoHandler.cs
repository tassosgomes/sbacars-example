using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Domain.Solicitacoes;

namespace SbaCars.Inventory.Application.Solicitacoes.AbrirSolicitacao;

public sealed class AbrirSolicitacaoHandler(
    IOfertaRepository ofertaRepository,
    ISolicitacaoRepository solicitacaoRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    CalculadoraDiasUteis calculadora) : ICommandHandler<AbrirSolicitacaoCommand, SolicitacaoDetalheResponse>
{
    public async Task<SolicitacaoDetalheResponse> HandleAsync(
        AbrirSolicitacaoCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var oferta = await ofertaRepository
            .ObterAsync(command.OfertaId, cancellationToken)
            .ConfigureAwait(false);

        if (oferta is null)
        {
            throw new OfertaNaoEncontradaException(command.OfertaId);
        }

        if (await solicitacaoRepository
                .ExistePendenteAsync(command.OfertaId, command.Tipo, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new SolicitacaoPendenteDuplicadaException(command.OfertaId, command.Tipo);
        }

        ValidatePreconditions(command, oferta);
        cancellationToken.ThrowIfCancellationRequested();

        var agora = clock.UtcNow;
        var autoria = new Autoria(
            currentUser.UserId ?? "system",
            currentUser.DisplayName ?? currentUser.UserId ?? "system",
            agora);
        var solicitacao = Solicitacao.Abrir(
            oferta.Id,
            command.Tipo,
            command.NovoPrecoCentavos,
            command.Justificativa!,
            autoria,
            agora);

        solicitacaoRepository.Adicionar(solicitacao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        InventoryMeters.Opened.Add(1, new KeyValuePair<string, object?>("tipo", command.Tipo.ToContractValue()));

        return SolicitacaoResponseMapper.ToDetalhe(
            solicitacao,
            oferta,
            calculadora,
            agora,
            currentUser.UserId);
    }

    private static void ValidatePreconditions(AbrirSolicitacaoCommand command, Oferta oferta)
    {
        switch (command.Tipo)
        {
            case TipoSolicitacao.Elegibilidade:
                if (oferta.Situacao == SituacaoOferta.Elegivel)
                {
                    throw new OfertaJaElegivelException();
                }

                var criterios = oferta.AvaliarCriteriosMinimos();
                if (criterios.Count > 0)
                {
                    throw new CriteriosMinimosNaoAtendidosException(criterios);
                }

                break;

            case TipoSolicitacao.Preco:
                if (oferta.PrecoOficial is null)
                {
                    throw new PrecoVigenteNaoDefinidoException();
                }

                break;

            case TipoSolicitacao.Retirada:
                if (oferta.Situacao == SituacaoOferta.Retirada)
                {
                    throw new OfertaJaRetiradaException();
                }

                break;

            case TipoSolicitacao.ReversaoVenda:
                if (oferta.Disponibilidade.Estado != EstadoDisponibilidade.Vendido)
                {
                    throw new ReversaoVendaNaoPermitidaException();
                }

                break;

            default:
                throw new TipoSolicitacaoNaoPermitidoException(command.Tipo.ToString());
        }
    }
}
