namespace SbaCars.Inventory.Domain.Ofertas;

public enum CodigoCriterio
{
    Identificacao,
    DadosBasicos,
    Localizacao,
    PrecoOficial,
    Disponibilidade,
    TransparenciaFatos,
}

public static class CodigoCriterioExtensions
{
    public static string ToContractValue(this CodigoCriterio codigo) => codigo switch
    {
        CodigoCriterio.Identificacao => "identificacao",
        CodigoCriterio.DadosBasicos => "dados-basicos",
        CodigoCriterio.Localizacao => "localizacao",
        CodigoCriterio.PrecoOficial => "preco-oficial",
        CodigoCriterio.Disponibilidade => "disponibilidade",
        CodigoCriterio.TransparenciaFatos => "transparencia-fatos",
        _ => throw new ArgumentOutOfRangeException(nameof(codigo), codigo, null),
    };
}
