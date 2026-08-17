namespace SbaCars.Inventory.Application.Common;

/// <summary>
/// Calculates elapsed business time for the Phase 1 validation SLA. Every hour on a Monday to
/// Friday is considered business time; weekends and no other calendar exceptions are excluded.
/// This deliberately models the contract's "seg-sex, sem feriados" rule rather than a local
/// machine's calendar.
/// </summary>
public sealed class CalculadoraDiasUteis
{
    private static readonly TimeSpan Sla = TimeSpan.FromDays(1);

    public bool ForaDoSla(DateTimeOffset abertaEm, DateTimeOffset agora)
        => CalcularTempoUtil(abertaEm, agora) > Sla;

    public TimeSpan CalcularTempoUtil(DateTimeOffset inicio, DateTimeOffset fim)
    {
        var cursor = inicio.ToUniversalTime();
        var limite = fim.ToUniversalTime();

        if (limite <= cursor)
        {
            return TimeSpan.Zero;
        }

        var tempoUtil = TimeSpan.Zero;
        while (cursor < limite)
        {
            if (IsDiaUtil(cursor.DayOfWeek))
            {
                var inicioDoProximoDia = cursor.Date.AddDays(1);
                var fimDoTrecho = limite < inicioDoProximoDia ? limite : inicioDoProximoDia;
                tempoUtil += fimDoTrecho - cursor;
                cursor = fimDoTrecho;
            }
            else
            {
                cursor = cursor.Date.AddDays(1);
            }
        }

        return tempoUtil;
    }

    private static bool IsDiaUtil(DayOfWeek dia) =>
        dia is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
}
