using Microsoft.Extensions.DependencyInjection;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Application.Cqrs;
using SbaCars.Inventory.Application.Common;

namespace SbaCars.Inventory.Application;

public static class InventoryApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services) =>
        services
            .AddSingleton<IClock, SystemClock>()
            .AddSingleton<CalculadoraDiasUteis>()
            .AddCqrs(typeof(InventoryApplicationMarker).Assembly);
}
