namespace SbaCars.Inventory.Domain.Ofertas;

public interface IOfertaRepository
{
    Task<Oferta?> ObterAsync(Guid ofertaId, CancellationToken cancellationToken = default);

    Task<bool> ExistePlacaAtivaAsync(
        string placa,
        Guid? ignorarOfertaId = null,
        CancellationToken cancellationToken = default);

    void Adicionar(Oferta oferta);

    void Remover(Oferta oferta);
}
