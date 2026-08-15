namespace SbaCars.BuildingBlocks.Web.Auth;

/// <summary>
/// The permission vocabulary from §5.4 of the architecture plan — Phase 1 only. Each constant is
/// both the ASP.NET Core authorization policy name and the business-side name of the Logto scope
/// it is projected from by <see cref="ScopeClaimsTransformation"/> (§5.6): the two happen to be
/// the same string today, but every controller, policy registration and test is written against
/// this constant, never against a raw claim value.
/// </summary>
public static class Permissoes
{
    /// <summary>Write access in inventory-service — offer curation, availability.</summary>
    public const string EstoqueGerenciar = "estoque:gerenciar";

    /// <summary>Operational read access in inventory-service.</summary>
    public const string EstoqueLer = "estoque:ler";

    /// <summary>Commercial content and media in catalog-service.</summary>
    public const string CatalogoGerenciar = "catalogo:gerenciar";

    /// <summary>Panel and continuity in interest-service.</summary>
    public const string AtendimentoGerenciar = "atendimento:gerenciar";

    /// <summary>
    /// Every Phase 1 permission, used once to register one authorization policy per permission
    /// (see <see cref="AuthExtensions.AddSbaCarsAuth"/>) without repeating the list. Phase 2 adds
    /// <c>compra:gerenciar</c> and <c>reserva:estender</c> here — and nowhere else.
    /// </summary>
    internal static readonly IReadOnlyCollection<string> All =
    [
        EstoqueGerenciar,
        EstoqueLer,
        CatalogoGerenciar,
        AtendimentoGerenciar,
    ];
}
