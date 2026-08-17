namespace SbaCars.Inventory.Domain.Solicitacoes;

/// <summary>Lifecycle of a validation request.</summary>
public enum StatusSolicitacao
{
    Pendente,
    Aprovada,
    Rejeitada,
}

public static class StatusSolicitacaoExtensions
{
    public static string ToContractValue(this StatusSolicitacao status) => status switch
    {
        StatusSolicitacao.Pendente => "pendente",
        StatusSolicitacao.Aprovada => "aprovada",
        StatusSolicitacao.Rejeitada => "rejeitada",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static StatusSolicitacao Parse(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "pendente" => StatusSolicitacao.Pendente,
        "aprovada" => StatusSolicitacao.Aprovada,
        "rejeitada" => StatusSolicitacao.Rejeitada,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Status de solicitação não suportado."),
    };
}
