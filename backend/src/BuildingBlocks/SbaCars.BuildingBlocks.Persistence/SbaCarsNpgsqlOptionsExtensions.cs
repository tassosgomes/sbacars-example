using Microsoft.EntityFrameworkCore;

namespace SbaCars.BuildingBlocks.Persistence;

/// <summary>
/// Applies the Npgsql wiring every service's <see cref="SbaCarsDbContext"/> must share (§4.2):
/// the migration history table living inside the service's own schema, snake_case naming and
/// resilience to transient Npgsql failures. Kept in one place so the four services behave
/// identically here — the same reasoning as the rest of BuildingBlocks (§3.3 of the plan).
/// </summary>
public static class SbaCarsNpgsqlOptionsExtensions
{
    /// <summary>
    /// Configures <paramref name="optionsBuilder"/> for a service's own schema.
    /// </summary>
    /// <remarks>
    /// Retry-on-failure is enabled here and nowhere combines it with an explicit transaction
    /// outside of an <see cref="Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy"/> — the
    /// one operation this base performs is <c>SaveChangesAsync</c>, which EF Core already wraps
    /// in the context's execution strategy. A future explicit transaction (e.g. to enlist the
    /// outbox in Phase B) must be opened through
    /// <c>Database.CreateExecutionStrategy().ExecuteAsync(...)</c>, never with
    /// <c>BeginTransactionAsync</c> called directly against a retrying context.
    /// </remarks>
    public static DbContextOptionsBuilder UseSbaCarsNpgsql(
        this DbContextOptionsBuilder optionsBuilder,
        string connectionString,
        string schema)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        optionsBuilder
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable(SbaCarsDbContext.MigrationsHistoryTableName, schema);
                npgsql.EnableRetryOnFailure();
            })
            .UseSnakeCaseNamingConvention();

        return optionsBuilder;
    }
}
