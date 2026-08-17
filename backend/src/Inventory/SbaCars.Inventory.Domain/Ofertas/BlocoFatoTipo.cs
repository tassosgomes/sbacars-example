namespace SbaCars.Inventory.Domain.Ofertas;

public enum BlocoFatoTipo
{
    Origem,
    Condicao,
    Historico,
}

public static class BlocoFatoTipoExtensions
{
    public static string ToContractValue(this BlocoFatoTipo tipo) => tipo switch
    {
        BlocoFatoTipo.Origem => "origem",
        BlocoFatoTipo.Condicao => "condicao",
        BlocoFatoTipo.Historico => "historico",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, null),
    };
}
