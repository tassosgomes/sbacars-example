using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace SbaCars.BuildingBlocks.Persistence;

/// <summary>
/// Replaces EF Core's default <see cref="IModelCacheKeyFactory"/>, which keys the compiled-model
/// cache by <see cref="DbContext"/> CLR type alone. Every service's own <c>DbContext</c> hardcodes
/// its schema in its constructor, so that default is harmless there — one CLR type, one schema,
/// forever. It stops being harmless the moment the *same* <see cref="SbaCarsDbContext"/> subclass
/// is built for two different schemas in the same process, which only test code does (a
/// schema-parametrized probe context reused across a positive-schema and a negative-schema case,
/// per §9's "prova que <c>svc_catalog</c> não lê <c>inventory</c>"): whichever schema compiles the
/// model first gets cached forever for that CLR type, and every later instance — regardless of the
/// schema string passed to its own constructor — silently reuses that first model. The symptom is
/// not an exception; it is generated SQL/DDL targeting the wrong schema.
/// </summary>
/// <remarks>
/// Registered once, for every <see cref="SbaCarsDbContext"/> in the codebase, by
/// <see cref="SbaCarsNpgsqlOptionsExtensions.UseSbaCarsNpgsql"/> — the single place every service's
/// (and every test probe's) options are built. See
/// <c>SbaCars.Persistence.IntegrationTests.ModelCacheKeyFactoryTests</c> for the regression test:
/// the same probe <c>DbContext</c> type built for two different schemas in one test, which was not
/// possible to write correctly before this fix (§12 of the architecture plan tracked this as an
/// open debt from A6b, which had worked around it by giving two test classes the same schema
/// instead of fixing the cache key).
/// </remarks>
public sealed class SchemaAwareModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        context is SbaCarsDbContext sbaCarsContext
            ? (context.GetType(), sbaCarsContext.ModelSchema, designTime)
            : (object)(context.GetType(), designTime);
}
