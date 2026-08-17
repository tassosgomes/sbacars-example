using Microsoft.EntityFrameworkCore;
using Npgsql;
using SbaCars.BuildingBlocks.Persistence;
using SbaCars.Inventory.Domain.Exceptions;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Domain.Solicitacoes;
using SbaCars.Inventory.Infrastructure.Solicitacoes;

namespace SbaCars.Inventory.Infrastructure;

/// <summary>
/// Owns the <c>inventory</c> schema and its aggregate sets. Business entities remain in Domain; only
/// their EF mapping is applied from Infrastructure.
/// </summary>
public sealed class InventoryDbContext : SbaCarsDbContext
{
    public const string Schema = "inventory";

    public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
        : base(options, Schema)
    {
    }

    public DbSet<Oferta> Ofertas => Set<Oferta>();

    public DbSet<Solicitacao> Solicitacoes => Set<Solicitacao>();

    public DbSet<Evidencia> Evidencias => Set<Evidencia>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        try
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch (DbUpdateException exception) when (IsPendingRequestUniqueViolation(exception))
        {
            throw CreatePendingRequestDuplicateException();
        }
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base
                .SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DbUpdateException exception) when (IsPendingRequestUniqueViolation(exception))
        {
            throw CreatePendingRequestDuplicateException();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }

    private static bool IsPendingRequestUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgresException &&
        postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
        string.Equals(
            postgresException.ConstraintName,
            SolicitacaoConfiguration.PendingTypeUniqueIndexName,
            StringComparison.Ordinal);

    private SolicitacaoPendenteDuplicadaException CreatePendingRequestDuplicateException()
    {
        var solicitacao = ChangeTracker
            .Entries<Solicitacao>()
            .FirstOrDefault(entry => entry.State == EntityState.Added)
            ?.Entity;

        return solicitacao is null
            ? new SolicitacaoPendenteDuplicadaException(Guid.Empty, default)
            : new SolicitacaoPendenteDuplicadaException(solicitacao.OfertaId, solicitacao.Tipo);
    }
}
