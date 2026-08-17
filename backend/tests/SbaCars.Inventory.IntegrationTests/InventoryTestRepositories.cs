using SbaCars.Inventory.Infrastructure;
using SbaCars.Inventory.Infrastructure.Ofertas;

namespace SbaCars.Inventory.IntegrationTests;

internal static class InventoryTestRepositories
{
    internal static OfertaRepository CreateOfertaRepository(InventoryDbContext context) =>
        new(context, new EvidenciaRepository(context));

    internal static EvidenciaRepository CreateEvidenciaRepository(InventoryDbContext context) =>
        new(context);
}
