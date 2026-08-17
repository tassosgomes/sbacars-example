using System.Collections.Frozen;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Application.Common;

public static class EvidenciaLookup
{
    public static async Task<IReadOnlyDictionary<Guid, Evidencia>> LoadMapForFatosAsync(
        FatosConhecidos fatos,
        IEvidenciaRepository evidenciaRepository,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fatos);
        ArgumentNullException.ThrowIfNull(evidenciaRepository);

        var ids = ColetarIds(fatos);
        if (ids.Count == 0)
        {
            return FrozenDictionary<Guid, Evidencia>.Empty;
        }

        var evidencias = await evidenciaRepository
            .ObterVariosAsync(ids, cancellationToken)
            .ConfigureAwait(false);

        return evidencias.ToDictionary(evidencia => evidencia.Id);
    }

    public static IReadOnlyCollection<Guid> ColetarIds(FatosConhecidos fatos)
    {
        ArgumentNullException.ThrowIfNull(fatos);

        var ids = new List<Guid>(3);
        ColetarId(fatos.Origem.EvidenciaId, ids);
        ColetarId(fatos.Condicao.EvidenciaId, ids);
        ColetarId(fatos.Historico.EvidenciaId, ids);
        return ids;
    }

    public static IReadOnlyCollection<Guid> ColetarIds(
        Guid? origemId,
        Guid? condicaoId,
        Guid? historicoId)
    {
        var ids = new List<Guid>(3);
        ColetarId(origemId, ids);
        ColetarId(condicaoId, ids);
        ColetarId(historicoId, ids);
        return ids;
    }

    private static void ColetarId(Guid? evidenciaId, List<Guid> ids)
    {
        if (evidenciaId is Guid id)
        {
            ids.Add(id);
        }
    }
}
