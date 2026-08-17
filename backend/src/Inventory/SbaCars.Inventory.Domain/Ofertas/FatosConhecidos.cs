using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Ofertas;

public sealed class FatosConhecidos : ValueObject
{
    private FatosConhecidos()
    {
        Origem = BlocoFato.Vazio(BlocoFatoTipo.Origem);
        Condicao = BlocoFato.Vazio(BlocoFatoTipo.Condicao);
        Historico = BlocoFato.Vazio(BlocoFatoTipo.Historico);
    }

    private FatosConhecidos(BlocoFato origem, BlocoFato condicao, BlocoFato historico)
    {
        Origem = RequireBlock(origem, BlocoFatoTipo.Origem);
        Condicao = RequireBlock(condicao, BlocoFatoTipo.Condicao);
        Historico = RequireBlock(historico, BlocoFatoTipo.Historico);
    }

    public BlocoFato Origem { get; private set; }

    public BlocoFato Condicao { get; private set; }

    public BlocoFato Historico { get; private set; }

    public static FatosConhecidos Vazios() => new(
        BlocoFato.Vazio(BlocoFatoTipo.Origem),
        BlocoFato.Vazio(BlocoFatoTipo.Condicao),
        BlocoFato.Vazio(BlocoFatoTipo.Historico));

    public static FatosConhecidos Criar(
        BlocoFato origem,
        BlocoFato condicao,
        BlocoFato historico) => new(origem, condicao, historico);

    public bool AtendeTransparencia => Origem.AtendeTransparencia &&
                                       Condicao.AtendeTransparencia &&
                                       Historico.AtendeTransparencia;

    protected override IEnumerable<object?> GetEqualityComponents() => [Origem, Condicao, Historico];

    private static BlocoFato RequireBlock(BlocoFato? block, BlocoFatoTipo expectedType)
    {
        ArgumentNullException.ThrowIfNull(block);

        if (block.Tipo != expectedType)
        {
            throw new ArgumentException(
                $"The fact block must be of type '{expectedType}'.",
                nameof(block));
        }

        return block;
    }
}
