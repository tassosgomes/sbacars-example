namespace SbaCars.Inventory.Domain.Solicitacoes;

/// <summary>Persistence port for the Solicitação aggregate.</summary>
public interface ISolicitacaoRepository
{
    Task<Solicitacao?> ObterAsync(
        Guid solicitacaoId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistePendenteAsync(
        Guid ofertaId,
        TipoSolicitacao tipo,
        CancellationToken cancellationToken = default);

    void Adicionar(Solicitacao solicitacao);
}
