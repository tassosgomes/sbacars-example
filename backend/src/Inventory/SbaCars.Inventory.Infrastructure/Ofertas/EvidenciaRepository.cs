using Microsoft.EntityFrameworkCore;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Infrastructure.Ofertas;

public sealed class EvidenciaRepository(InventoryDbContext context)
    : Repository<Evidencia>(context), IEvidenciaRepository
{
    private InventoryDbContext InventoryContext => (InventoryDbContext)Context;

    public Task<Evidencia?> ObterAsync(
        Guid evidenciaId,
        CancellationToken cancellationToken = default) =>
        InventoryContext.Evidencias
            .AsNoTracking()
            .SingleOrDefaultAsync(evidencia => evidencia.Id == evidenciaId, cancellationToken);

    public async Task<IReadOnlyList<Evidencia>> ObterVariosAsync(
        IEnumerable<Guid> evidenciaIds,
        CancellationToken cancellationToken = default)
    {
        var ids = evidenciaIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return [];
        }

        return await InventoryContext.Evidencias
            .AsNoTracking()
            .Where(evidencia => ids.Contains(evidencia.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public void Adicionar(Evidencia evidencia) => Add(evidencia);
}
