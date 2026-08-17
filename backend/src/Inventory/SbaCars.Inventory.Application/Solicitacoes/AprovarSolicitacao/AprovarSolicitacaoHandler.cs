using Microsoft.Extensions.Logging;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Integracao;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Domain.Solicitacoes;

namespace SbaCars.Inventory.Application.Solicitacoes.AprovarSolicitacao;

public sealed class AprovarSolicitacaoHandler(
    ISolicitacaoRepository solicitacaoRepository,
    IOfertaRepository ofertaRepository,
    IUnitOfWork unitOfWork,
    IEstoqueIntegrationEventPublisher integrationEventPublisher,
    ICurrentUser currentUser,
    IClock clock,
    CalculadoraDiasUteis calculadora,
    ILogger<AprovarSolicitacaoHandler> logger) : ICommandHandler<AprovarSolicitacaoCommand, SolicitacaoDetalheResponse>
{
    public async Task<SolicitacaoDetalheResponse> HandleAsync(
        AprovarSolicitacaoCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        using var activity = InventoryActivitySource.Instance.StartActivity(
            "inventory.solicitacao.aprovar");
        activity?.SetTag("solicitacao.id", command.SolicitacaoId);

        var solicitacao = await solicitacaoRepository
            .ObterAsync(command.SolicitacaoId, cancellationToken)
            .ConfigureAwait(false);

        if (solicitacao is null)
        {
            throw new SolicitacaoNaoEncontradaException(command.SolicitacaoId);
        }

        EnsureCanApprove(solicitacao);

        var oferta = await ofertaRepository
            .ObterAsync(solicitacao.OfertaId, cancellationToken)
            .ConfigureAwait(false);

        if (oferta is null)
        {
            throw new OfertaNaoEncontradaException(solicitacao.OfertaId);
        }

        var agora = clock.UtcNow;
        var autoria = CreateDecisionAuthorship(agora);
        var situacaoAnterior = oferta.Situacao;
        activity?.SetTag("solicitacao.tipo", solicitacao.Tipo.ToContractValue());
        activity?.SetTag("oferta.id", oferta.Id);

        ApplyApprovedChange(solicitacao, oferta, autoria, agora);
        solicitacao.Aprovar(autoria, agora, command.Observacao);

        cancellationToken.ThrowIfCancellationRequested();
        await PublishApprovedEventAsync(
            solicitacao,
            oferta,
            situacaoAnterior,
            agora,
            cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var duration = DecisionDuration(solicitacao.AbertaEm, agora);
        InventoryMeters.TimeToDecision.Record(
            duration.TotalSeconds,
            new KeyValuePair<string, object?>("tipo", solicitacao.Tipo.ToContractValue()),
            new KeyValuePair<string, object?>("status", StatusSolicitacao.Aprovada.ToContractValue()));
        logger.LogInformation(
            "Solicitação {SolicitacaoId} do tipo {Tipo} da oferta {OfertaId} aprovada por {DecididaPor} após {DuracaoPendenteMs} ms.",
            solicitacao.Id,
            solicitacao.Tipo.ToContractValue(),
            solicitacao.OfertaId,
            solicitacao.Decisao?.DecididaPor.UsuarioId,
            duration.TotalMilliseconds);

        return SolicitacaoResponseMapper.ToDetalhe(
            solicitacao,
            oferta,
            calculadora,
            agora,
            currentUser.UserId);
    }

    private void EnsureCanApprove(Solicitacao solicitacao)
    {
        if (solicitacao.Status != StatusSolicitacao.Pendente)
        {
            throw new SolicitacaoJaDecididaException(solicitacao.Id);
        }

        if (string.Equals(
                solicitacao.AbertaPor.UsuarioId,
                currentUser.UserId,
                StringComparison.Ordinal))
        {
            throw new AutoAprovacaoException();
        }
    }

    private Autoria CreateDecisionAuthorship(DateTimeOffset agora) => new(
        currentUser.UserId ?? "system",
        currentUser.DisplayName ?? currentUser.UserId ?? "system",
        agora);

    private static void ApplyApprovedChange(
        Solicitacao solicitacao,
        Oferta oferta,
        Autoria autoria,
        DateTimeOffset agora)
    {
        switch (solicitacao.Tipo)
        {
            case TipoSolicitacao.Elegibilidade:
                oferta.TornarElegivel(autoria, agora);
                break;
            case TipoSolicitacao.Preco:
                oferta.AplicarAlteracaoDePreco(solicitacao.NovoPrecoCentavos!.Value, autoria, agora);
                break;
            case TipoSolicitacao.Retirada:
                oferta.Retirar(autoria, agora);
                break;
            case TipoSolicitacao.ReversaoVenda:
                oferta.ReverterVenda(autoria, agora);
                break;
            default:
                throw new TipoSolicitacaoNaoPermitidoException(solicitacao.Tipo.ToString());
        }
    }

    private async Task PublishApprovedEventAsync(
        Solicitacao solicitacao,
        Oferta oferta,
        SituacaoOferta situacaoAnterior,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        switch (solicitacao.Tipo)
        {
            case TipoSolicitacao.Elegibilidade when situacaoAnterior == SituacaoOferta.Suspensa:
                await integrationEventPublisher.PublishOfferUpdatedAsync(
                    oferta.Id,
                    occurredAt,
                    cancellationToken).ConfigureAwait(false);
                break;
            case TipoSolicitacao.Elegibilidade:
                await integrationEventPublisher.PublishOfferIncludedAsync(
                    oferta.Id,
                    occurredAt,
                    cancellationToken).ConfigureAwait(false);
                break;
            case TipoSolicitacao.Preco:
                await integrationEventPublisher.PublishOfferUpdatedAsync(
                    oferta.Id,
                    occurredAt,
                    cancellationToken).ConfigureAwait(false);
                break;
            case TipoSolicitacao.Retirada:
                await integrationEventPublisher.PublishOfferWithdrawnAsync(
                    oferta.Id,
                    occurredAt,
                    cancellationToken).ConfigureAwait(false);
                break;
            case TipoSolicitacao.ReversaoVenda:
                await integrationEventPublisher.PublishAvailabilityChangedAsync(
                    oferta.Id,
                    oferta.Disponibilidade.Estado.ToContractValue(),
                    occurredAt,
                    cancellationToken).ConfigureAwait(false);
                break;
            default:
                throw new TipoSolicitacaoNaoPermitidoException(solicitacao.Tipo.ToString());
        }
    }

    private static TimeSpan DecisionDuration(DateTimeOffset abertaEm, DateTimeOffset decididaEm) =>
        decididaEm >= abertaEm ? decididaEm - abertaEm : TimeSpan.Zero;
}
