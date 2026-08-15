using Microsoft.EntityFrameworkCore;
using SbaCars.BuildingBlocks.Persistence.Auditing;

namespace SbaCars.BuildingBlocks.Persistence;

/// <summary>
/// Base <see cref="DbContext"/> shared by every service. Centralizes the conventions that must
/// be identical across the four services (§4.2 of the foundation plan) so that a service's own
/// <c>DbContext</c> only has to declare its schema and its <c>DbSet</c>s.
/// </summary>
/// <remarks>
/// Concrete implementations live exclusively in each service's Infrastructure project — never in
/// Api, Application or Domain — because <c>DbContext</c> is an EF Core/persistence concern, not a
/// business one.
/// </remarks>
public abstract class SbaCarsDbContext : DbContext
{
    /// <summary>
    /// Name shared by every service's migration history table. Combined with the service's own
    /// schema (never <c>public</c>), so each service's migration history lives where the rest of
    /// its data lives.
    /// </summary>
    public const string MigrationsHistoryTableName = "__ef_migrations_history";

    private readonly string _schema;
    private readonly SensitiveDataAuditInterceptor? _sensitiveDataAuditInterceptor;

    protected SbaCarsDbContext(DbContextOptions options, string schema)
        : this(options, schema, sensitiveDataAuditInterceptor: null)
    {
    }

    /// <summary>
    /// Overload used by a service once it has its first <see cref="Domain.ISensitiveDataEntity"/>
    /// (§5.7). <paramref name="sensitiveDataAuditInterceptor"/> must be the very same instance
    /// already passed to <c>optionsBuilder.AddInterceptors(...)</c> when building
    /// <paramref name="options"/> — registering it on the options is what makes EF Core actually
    /// invoke it; passing it here as well is what lets this context flush the read accesses it
    /// buffered (see <see cref="FlushSensitiveDataAuditAsync"/>).
    /// </summary>
    protected SbaCarsDbContext(
        DbContextOptions options,
        string schema,
        SensitiveDataAuditInterceptor? sensitiveDataAuditInterceptor)
        : base(options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        _schema = schema;
        _sensitiveDataAuditInterceptor = sensitiveDataAuditInterceptor;

        // No-tracking is the default for the whole context (§4.2): a use case that needs
        // tracking for a write opts in explicitly with `.AsTracking()` on that one query, or by
        // calling `Attach`/`Add`/`Update` directly — it is never the ambient default.
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        // Lazy loading is never enabled anywhere in this hierarchy — no lazy-loading proxies
        // package is referenced, and no `virtual` navigation property pattern is used, so there
        // is nothing here to explicitly turn off.
    }

    /// <summary>
    /// Persists any sensitive-data read accesses buffered by
    /// <see cref="SensitiveDataAuditInterceptor"/> for this context instance. A no-op when no
    /// interceptor was supplied to the constructor, or when nothing is pending — safe to call
    /// after every read regardless of whether the entity being read is sensitive.
    /// </summary>
    /// <remarks>
    /// Called automatically by <see cref="Repository{TEntity}.FindAsync"/> after every fetch. A
    /// query that bypasses the repository (raw LINQ against a <c>DbSet</c>, a custom read method)
    /// must call this itself, or rely on this same context's next business
    /// <c>SaveChangesAsync</c> to flush the buffer instead — see the interceptor's remarks for why
    /// the read cannot be persisted the instant it happens.
    /// </remarks>
    public async Task FlushSensitiveDataAuditAsync(CancellationToken cancellationToken = default)
    {
        if (_sensitiveDataAuditInterceptor is null)
        {
            return;
        }

        _sensitiveDataAuditInterceptor.AppendPendingAuditEntries(this);

        if (ChangeTracker.HasChanges())
        {
            await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(_schema);
        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // DateTimeOffset always round-trips as UTC (§4.2): a value with a non-zero offset would
        // still map to `timestamptz` correctly at the SQL level, but silently disagreeing with
        // "always UTC, sourced from IClock" is exactly the kind of bug that only shows up when
        // two services compare timestamps. Normalizing on the way in makes the rule structural
        // instead of a code-review reminder.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcDateTimeOffsetConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<NullableUtcDateTimeOffsetConverter>();
    }
}
