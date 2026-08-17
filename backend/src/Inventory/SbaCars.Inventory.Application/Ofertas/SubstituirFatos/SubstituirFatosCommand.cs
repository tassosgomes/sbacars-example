using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Application.Ofertas.SubstituirFatos;

public sealed record SubstituirFatosCommand : ICommand<OfertaDetalheResponse>
{
    public Guid OfertaId { get; init; }

    public BlocoFatoInput? Origem { get; init; }

    public BlocoFatoInput? Condicao { get; init; }

    public BlocoFatoInput? Historico { get; init; }

    public bool ConfirmaSuspensao { get; init; }

    public FatosConhecidos ToDomain(Autoria autoria) => FatosConhecidos.Criar(
        RequireBlock(Origem).ToDomain(BlocoFatoTipo.Origem, autoria),
        RequireBlock(Condicao).ToDomain(BlocoFatoTipo.Condicao, autoria),
        RequireBlock(Historico).ToDomain(BlocoFatoTipo.Historico, autoria));

    private static BlocoFatoInput RequireBlock(BlocoFatoInput? block) =>
        block ?? throw new ArgumentNullException(nameof(block));
}

/// <summary>Input for one of the three known-fact blocks.</summary>
public sealed record BlocoFatoInput
{
    public bool Indisponivel { get; init; }

    public string? Descricao { get; init; }

    public string? Fonte { get; init; }

    public string? LimitacaoDeclarada { get; init; }

    /// <summary>
    /// Reserved for the evidence slice. V-04 does not persist or resolve evidence metadata.
    /// </summary>
    public Guid? EvidenciaId { get; init; }

    public BlocoFato ToDomain(BlocoFatoTipo tipo, Autoria autoria) => new(
        tipo,
        Indisponivel,
        Descricao,
        Fonte,
        LimitacaoDeclarada,
        EvidenciaId,
        autoria);
}
