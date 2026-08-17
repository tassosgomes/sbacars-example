namespace SbaCars.Inventory.Domain.Ofertas;

public interface IEvidenciaRepository
{
    Task<Evidencia?> ObterAsync(Guid evidenciaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Evidencia>> ObterVariosAsync(
        IEnumerable<Guid> evidenciaIds,
        CancellationToken cancellationToken = default);

    void Adicionar(Evidencia evidencia);
}
