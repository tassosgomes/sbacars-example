using SbaCars.BuildingBlocks.Domain;
using SbaCars.Inventory.Domain.Exceptions;

namespace SbaCars.Inventory.Domain.Solicitacoes;

/// <summary>Change that requires a validation decision before it is applied.</summary>
public enum TipoSolicitacao
{
    Elegibilidade,
    Preco,
    Retirada,
    ReversaoVenda,
}

public static class TipoSolicitacaoExtensions
{
    public static string ToContractValue(this TipoSolicitacao tipo) => tipo switch
    {
        TipoSolicitacao.Elegibilidade => "elegibilidade",
        TipoSolicitacao.Preco => "preco",
        TipoSolicitacao.Retirada => "retirada",
        TipoSolicitacao.ReversaoVenda => "reversao-venda",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, null),
    };

    public static TipoSolicitacao Parse(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "elegibilidade" => TipoSolicitacao.Elegibilidade,
        "preco" => TipoSolicitacao.Preco,
        "retirada" => TipoSolicitacao.Retirada,
        "reversao-venda" => TipoSolicitacao.ReversaoVenda,
        _ => throw new TipoSolicitacaoNaoPermitidoException(value),
    };
}
