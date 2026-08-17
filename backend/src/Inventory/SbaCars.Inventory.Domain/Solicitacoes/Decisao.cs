using SbaCars.BuildingBlocks.Domain;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Domain.Solicitacoes;

/// <summary>
/// Immutable record of the decision made for a validation request.
/// </summary>
public sealed class Decisao : ValueObject
{
    private Decisao()
    {
        DecididaPor = Autoria.System(DateTimeOffset.UnixEpoch);
    }

    private Decisao(
        StatusSolicitacao status,
        DateTimeOffset decididaEm,
        Autoria decididaPor,
        string? justificativa)
    {
        if (status is not (StatusSolicitacao.Aprovada or StatusSolicitacao.Rejeitada))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "A decisão deve ser aprovada ou rejeitada.");
        }

        ArgumentNullException.ThrowIfNull(decididaPor);

        if (status == StatusSolicitacao.Rejeitada && string.IsNullOrWhiteSpace(justificativa))
        {
            throw new JustificativaRejeicaoObrigatoriaException();
        }

        Status = status;
        DecididaEm = decididaEm.ToUniversalTime();
        DecididaPor = decididaPor.Copiar();
        Justificativa = Normalize(justificativa);
    }

    public StatusSolicitacao Status { get; private set; }

    public DateTimeOffset DecididaEm { get; private set; }

    public Autoria DecididaPor { get; private set; }

    /// <summary>
    /// Optional observation for an approval and the mandatory reason for a rejection.
    /// </summary>
    public string? Justificativa { get; private set; }

    public static Decisao Aprovar(
        Autoria decididaPor,
        DateTimeOffset decididaEm,
        string? observacao) =>
        new(StatusSolicitacao.Aprovada, decididaEm, decididaPor, observacao);

    public static Decisao Rejeitar(
        Autoria decididaPor,
        DateTimeOffset decididaEm,
        string justificativa) =>
        new(StatusSolicitacao.Rejeitada, decididaEm, decididaPor, justificativa);

    protected override IEnumerable<object?> GetEqualityComponents() =>
        [Status, DecididaEm, DecididaPor, Justificativa];

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
