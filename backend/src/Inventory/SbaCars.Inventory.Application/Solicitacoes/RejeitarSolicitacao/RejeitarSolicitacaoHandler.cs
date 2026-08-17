using Microsoft.Extensions.Logging;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Domain.Solicitacoes;

namespace SbaCars.Inventory.Application.Solicitacoes.RejeitarSolicitacao;

public sealed class RejeitarSolicitacaoHandler(
    ISolicitacaoRepository solicitacaoRepository,
    IOfertaRepository ofertaRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    CalculadoraDiasUteis calculadora,
    ILogger<RejeitarSolicitacaoHandler> logger) : ICommandHandler<RejeitarSolicitacaoCommand, SolicitacaoDetalheResponse>
{
    public async Task<SolicitacaoDetalheResponse> HandleAsync(
        RejeitarSolicitacaoCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        using var activity = InventoryActivitySource.Instance.StartActivity(
            "inventory.solicitacao.rejeitar");
        activity?.SetTag("solicitacao.id", command.SolicitacaoId);

        var solicitacao = await solicitacaoRepository
            .ObterAsync(command.SolicitacaoId, cancellationToken)
            .ConfigureAwait(false);

        if (solicitacao is null)
        {
            throw new SolicitacaoNaoEncontradaException(command.SolicitacaoId);
        }

        if (solicitacao.Status != StatusSolicitacao.Pendente)
        {
            throw new SolicitacaoJaDecididaException(solicitacao.Id);
        }

        var oferta = await ofertaRepository
            .ObterAsync(solicitacao.OfertaId, cancellationToken)
            .ConfigureAwait(false);

        if (oferta is null)
        {
            throw new OfertaNaoEncontradaException(solicitacao.OfertaId);
        }

        var agora = clock.UtcNow;
        var autoria = new Autoria(
            currentUser.UserId ?? "system",
            currentUser.DisplayName ?? currentUser.UserId ?? "system",
            agora);
        activity?.SetTag("solicitacao.tipo", solicitacao.Tipo.ToContractValue());
        activity?.SetTag("oferta.id", oferta.Id);

        solicitacao.Rejeitar(autoria, agora, command.Justificativa!);

        cancellationToken.ThrowIfCancellationRequested();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var duration = DecisionDuration(solicitacao.AbertaEm, agora);
        InventoryMeters.TimeToDecision.Record(
            duration.TotalSeconds,
            new KeyValuePair<string, object?>("tipo", solicitacao.Tipo.ToContractValue()),
            new KeyValuePair<string, object?>("status", StatusSolicitacao.Rejeitada.ToContractValue()));
        logger.LogInformation(
            "Solicitação {SolicitacaoId} do tipo {Tipo} da oferta {OfertaId} rejeitada por {DecididaPor} após {DuracaoPendenteMs} ms.",
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

    private static TimeSpan DecisionDuration(DateTimeOffset abertaEm, DateTimeOffset decididaEm) =>
        decididaEm >= abertaEm ? decididaEm - abertaEm : TimeSpan.Zero;
}
