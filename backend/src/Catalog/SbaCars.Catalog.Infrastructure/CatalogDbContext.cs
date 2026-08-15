using Microsoft.EntityFrameworkCore;
using SbaCars.BuildingBlocks.Persistence;

namespace SbaCars.Catalog.Infrastructure;

/// <summary>
/// Entry point to the <c>catalog</c> schema (D01 — Catálogo e Descoberta). Empty of
/// business <c>DbSet</c>s for now: this task (A4) only proves the persistence pipeline end to
/// end — schema, role, migration history — ahead of the first aggregate.
/// </summary>
public sealed class CatalogDbContext : SbaCarsDbContext
{
    public const string Schema = "catalog";

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
        : base(options, Schema)
    {
    }
}
