using Microsoft.EntityFrameworkCore;
using SbaCars.BuildingBlocks.Persistence;

namespace SbaCars.Purchase.Infrastructure;

/// <summary>
/// Entry point to the <c>purchase</c> schema (D04 — Compra Assistida e Financiamento, Fase 2).
/// Empty of
/// business <c>DbSet</c>s for now: this task (A4) only proves the persistence pipeline end to
/// end — schema, role, migration history — ahead of the first aggregate.
/// </summary>
public sealed class PurchaseDbContext : SbaCarsDbContext
{
    public const string Schema = "purchase";

    public PurchaseDbContext(DbContextOptions<PurchaseDbContext> options)
        : base(options, Schema)
    {
    }
}
