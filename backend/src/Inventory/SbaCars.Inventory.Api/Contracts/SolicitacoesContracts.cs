using SbaCars.Inventory.Application.Solicitacoes.AbrirSolicitacao;
using SbaCars.Inventory.Application.Solicitacoes.AprovarSolicitacao;
using SbaCars.Inventory.Application.Solicitacoes.RejeitarSolicitacao;
using SbaCars.Inventory.Domain.Solicitacoes;

namespace SbaCars.Inventory.Api.Contracts;

public sealed record AbrirSolicitacaoRequest
{
    public string? Tipo { get; init; }

    public long? NovoPrecoCentavos { get; init; }

    public string? Justificativa { get; init; }

    public AbrirSolicitacaoCommand ToCommand(Guid ofertaId) => new()
    {
        OfertaId = ofertaId,
        Tipo = TipoSolicitacaoExtensions.Parse(Tipo),
        NovoPrecoCentavos = NovoPrecoCentavos,
        Justificativa = Justificativa,
    };
}

public sealed record AprovarSolicitacaoRequest
{
    public string? Observacao { get; init; }

    public AprovarSolicitacaoCommand ToCommand(Guid solicitacaoId) => new()
    {
        SolicitacaoId = solicitacaoId,
        Observacao = Observacao,
    };
}

public sealed record RejeitarSolicitacaoRequest
{
    public string? Justificativa { get; init; }

    public RejeitarSolicitacaoCommand ToCommand(Guid solicitacaoId) => new()
    {
        SolicitacaoId = solicitacaoId,
        Justificativa = Justificativa,
    };
}
