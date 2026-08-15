using Microsoft.EntityFrameworkCore;
using SbaCars.BuildingBlocks.Persistence;

namespace SbaCars.Inventory.Infrastructure;

/// <summary>
/// Entry point to the <c>inventory</c> schema (D02 — Estoque Curado e Disponibilidade). Empty of
/// business <c>DbSet</c>s for now: this task (A4) only proves the persistence pipeline end to
/// end — schema, role, migration history — ahead of the first aggregate.
/// </summary>
public sealed class InventoryDbContext : SbaCarsDbContext
{
    public const string Schema = "inventory";

    public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
        : base(options, Schema)
    {
    }
}
