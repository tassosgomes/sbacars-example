namespace SbaCars.Inventory.Domain.Ofertas;

public enum SituacaoOferta
{
    EmPreparacao,
    Elegivel,
    Suspensa,
    Retirada,
}

public static class SituacaoOfertaExtensions
{
    public static string ToContractValue(this SituacaoOferta situacao) => situacao switch
    {
        SituacaoOferta.EmPreparacao => "em-preparacao",
        SituacaoOferta.Elegivel => "elegivel",
        SituacaoOferta.Suspensa => "suspensa",
        SituacaoOferta.Retirada => "retirada",
        _ => throw new ArgumentOutOfRangeException(nameof(situacao), situacao, null),
    };
}
