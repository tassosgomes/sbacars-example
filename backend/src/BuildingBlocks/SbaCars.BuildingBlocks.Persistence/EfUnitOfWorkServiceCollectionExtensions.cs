using Microsoft.Extensions.DependencyInjection;
using SbaCars.BuildingBlocks.Application;

namespace SbaCars.BuildingBlocks.Persistence;

/// <summary>
/// Registers <see cref="EfUnitOfWork{TContext}"/> as both <see cref="IUnitOfWork"/> and
/// <see cref="IOutboxTransaction"/> for a service's own <typeparamref name="TContext"/>.
/// </summary>
public static class EfUnitOfWorkServiceCollectionExtensions
{
    public static IServiceCollection AddEfUnitOfWork<TContext>(this IServiceCollection services)
        where TContext : SbaCarsDbContext
    {
        services.AddScoped<EfUnitOfWork<TContext>>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<EfUnitOfWork<TContext>>());
        services.AddScoped<IOutboxTransaction>(provider => provider.GetRequiredService<EfUnitOfWork<TContext>>());
        return services;
    }
}
