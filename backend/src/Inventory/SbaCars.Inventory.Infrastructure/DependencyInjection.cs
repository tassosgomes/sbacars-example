using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.BuildingBlocks.Persistence.Auditing;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Integracao;
using SbaCars.Inventory.Application.Ofertas.ListarOfertas;
using SbaCars.Inventory.Application.Solicitacoes;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Domain.Solicitacoes;
using SbaCars.Inventory.Infrastructure.Ofertas;
using SbaCars.Inventory.Infrastructure.Solicitacoes;
using SbaCars.Inventory.Infrastructure.Storage;

namespace SbaCars.Inventory.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Wires the <c>inventory</c> schema's persistence: <see cref="InventoryDbContext"/> against
    /// the application role (DML only), the matching <see cref="IUnitOfWork"/>, and the
    /// end-of-request sensitive-data audit flush (§5.7 — a no-op today, since this service has no
    /// <c>ISensitiveDataEntity</c> yet). Connection string is bound through
    /// <see cref="PersistenceOptions"/> with <c>ValidateOnStart</c> — missing configuration fails
    /// the process at boot (§4.4).
    /// </summary>
    public static IServiceCollection AddInventoryInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSbaCarsPersistenceOptions(configuration);

        services.AddDbContext<InventoryDbContext>((provider, options) =>
        {
            var persistence = provider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            options.UseSbaCarsNpgsql(persistence.ConnectionString, InventoryDbContext.Schema);
        });

        services.AddEfUnitOfWork<InventoryDbContext>();
        services.AddSbaCarsSensitiveDataAuditFlusher<InventoryDbContext>();
        services.AddScoped<IOfertaRepository, OfertaRepository>();
        services.AddScoped<IOfertaReadRepository, OfertaRepository>();
        services.AddScoped<IOfertaElegivelReadRepository, OfertaRepository>();
        services.AddScoped<IEvidenciaRepository, EvidenciaRepository>();
        services.AddSingleton<IInventoryStorageSettings, InventoryStorageSettings>();
        services.AddScoped<ISolicitacaoRepository, SolicitacaoRepository>();
        services.AddScoped<ISolicitacaoReadRepository, SolicitacaoRepository>();
        services.AddSingleton<EstoqueIntegrationEventFactory>();
        services.AddScoped<IEstoqueIntegrationEventPublisher, EstoqueIntegrationEventPublisher>();

        // A8 (§8): Npgsql's own tracing, composed onto whatever tracer provider
        // AddSbaCarsObservability registers in Program.cs — see the remark on this project's
        // .csproj for why it lives here rather than in BuildingBlocks.Observability.
        services.ConfigureOpenTelemetryTracerProvider((_, tracing) => tracing.AddNpgsql());
        services.ConfigureOpenTelemetryTracerProvider((_, tracing) =>
            tracing.AddSource(SbaCars.Inventory.Application.Common.InventoryActivitySource.Name));
        services.ConfigureOpenTelemetryMeterProvider((_, metrics) =>
            metrics.AddMeter(SbaCars.Inventory.Application.Common.InventoryMeters.Name));

        return services;
    }
}
