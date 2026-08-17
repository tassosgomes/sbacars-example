using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Application.Common;

public static class OfertaDetalheAssembler
{
    public static async Task<OfertaDetalheResponse> BuildAsync(
        Oferta oferta,
        IEvidenciaRepository evidenciaRepository,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(oferta);
        ArgumentNullException.ThrowIfNull(evidenciaRepository);

        var evidencias = await EvidenciaLookup
            .LoadMapForFatosAsync(oferta.Fatos, evidenciaRepository, cancellationToken)
            .ConfigureAwait(false);

        return OfertaResponseMapper.ToDetalhe(oferta, evidencias);
    }
}
