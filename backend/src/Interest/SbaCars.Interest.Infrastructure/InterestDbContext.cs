using Microsoft.EntityFrameworkCore;
using SbaCars.BuildingBlocks.Persistence;

namespace SbaCars.Interest.Infrastructure;

/// <summary>
/// Entry point to the <c>interest</c> schema (D03 — Interesse e Atendimento). Empty of
/// business <c>DbSet</c>s for now: this task (A4) only proves the persistence pipeline end to
/// end — schema, role, migration history — ahead of the first aggregate.
/// </summary>
public sealed class InterestDbContext : SbaCarsDbContext
{
    public const string Schema = "interest";

    public InterestDbContext(DbContextOptions<InterestDbContext> options)
        : base(options, Schema)
    {
    }
}
