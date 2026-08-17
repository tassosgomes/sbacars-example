using SbaCars.BuildingBlocks.Domain;
using SbaCars.Inventory.Domain.Exceptions;

namespace SbaCars.Inventory.Domain.Ofertas;

/// <summary>A known fact or an explicit declaration that the information is unavailable.</summary>
public sealed class BlocoFato : ValueObject
{
    private BlocoFato()
    {
        AtualizadoPor = Autoria.System(DateTimeOffset.UnixEpoch);
    }

    public BlocoFato(
        BlocoFatoTipo tipo,
        bool indisponivel = false,
        string? descricao = null,
        string? fonte = null,
        string? limitacaoDeclarada = null,
        Guid? evidenciaId = null,
        Autoria? atualizadoPor = null)
    {
        if (indisponivel && string.IsNullOrWhiteSpace(limitacaoDeclarada))
        {
            throw new LimitacaoNaoDeclaradaException(tipo);
        }

        Tipo = tipo;
        Indisponivel = indisponivel;
        // An unavailable block is an explicit declaration that the content is not known. Any
        // content sent alongside that declaration is intentionally discarded, including fields
        // that will later hold an evidence reference (V-11).
        Descricao = indisponivel ? null : Normalize(descricao);
        Fonte = indisponivel ? null : Normalize(fonte);
        EvidenciaId = indisponivel ? null : evidenciaId;
        LimitacaoDeclarada = Normalize(limitacaoDeclarada);
        AtualizadoPor = (atualizadoPor ?? Autoria.System(DateTimeOffset.UnixEpoch)).Copiar();
    }

    public BlocoFatoTipo Tipo { get; private set; }

    public bool Indisponivel { get; private set; }

    public string? Descricao { get; private set; }

    public string? Fonte { get; private set; }

    public string? LimitacaoDeclarada { get; private set; }

    public Guid? EvidenciaId { get; private set; }

    public Autoria AtualizadoPor { get; private set; }

    public bool AtendeTransparencia =>
        !string.IsNullOrWhiteSpace(Descricao) ||
        !string.IsNullOrWhiteSpace(Fonte) ||
        !string.IsNullOrWhiteSpace(LimitacaoDeclarada);

    public static BlocoFato Vazio(BlocoFatoTipo tipo) =>
        new(tipo, false, null, null, null, null, Autoria.System(DateTimeOffset.UnixEpoch), true);

    private BlocoFato(
        BlocoFatoTipo tipo,
        bool indisponivel,
        string? descricao,
        string? fonte,
        string? limitacaoDeclarada,
        Guid? evidenciaId,
        Autoria atualizadoPor,
        bool allowEmpty)
    {
        Tipo = tipo;
        Indisponivel = indisponivel;
        Descricao = indisponivel ? null : Normalize(descricao);
        Fonte = indisponivel ? null : Normalize(fonte);
        EvidenciaId = indisponivel ? null : evidenciaId;
        LimitacaoDeclarada = Normalize(limitacaoDeclarada);
        AtualizadoPor = atualizadoPor;
    }

    protected override IEnumerable<object?> GetEqualityComponents() =>
        [Tipo, Indisponivel, Descricao, Fonte, LimitacaoDeclarada, EvidenciaId, AtualizadoPor];

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
