using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace SbaCars.Inventory.Application.Common;

/// <summary>Operational gauges for the validation queue.</summary>
public static class InventoryMeters
{
    public const string Name = "inventory-service";

    private static readonly Meter Meter = new(Name);
    private static readonly ConcurrentDictionary<string, long> PendingByType = new(StringComparer.Ordinal);
    private static long _pendingOutsideSla;

    public static readonly ObservableGauge<long> Pending = Meter.CreateObservableGauge(
        "inventory.solicitacoes.pendentes",
        ObservePending,
        description: "Pending validation requests grouped by type.");

    public static readonly ObservableGauge<long> OutsideSla = Meter.CreateObservableGauge(
        "inventory.solicitacoes.fora_sla",
        () => Volatile.Read(ref _pendingOutsideSla),
        description: "Pending validation requests outside the one-business-day SLA.");

    public static readonly Counter<long> Opened = Meter.CreateCounter<long>(
        "inventory.solicitacoes.abertas",
        description: "Validation requests opened.");

    public static readonly Histogram<double> TimeToDecision = Meter.CreateHistogram<double>(
        "inventory.solicitacoes.tempo_ate_decisao",
        unit: "s",
        description: "Elapsed time from opening a validation request to its decision.");

    /// <summary>
    /// Counts inventory integration events after the foundation publisher successfully stages
    /// them in the current outbox session. The <c>tipo</c> tag carries the contract wire name.
    /// </summary>
    public static readonly Counter<long> EventPublished = Meter.CreateCounter<long>(
        "inventory.evento.publicado",
        description: "Integration events successfully staged for publication, grouped by type.");

    public static void SetPendingSnapshot(
        IReadOnlyDictionary<string, int> pendingByType,
        int outsideSla)
    {
        ArgumentNullException.ThrowIfNull(pendingByType);

        foreach (var tipo in pendingByType.Keys)
        {
            PendingByType[tipo] = pendingByType[tipo];
        }

        Interlocked.Exchange(ref _pendingOutsideSla, outsideSla);
    }

    private static IEnumerable<Measurement<long>> ObservePending()
    {
        foreach (var item in PendingByType)
        {
            yield return new Measurement<long>(
                item.Value,
                new KeyValuePair<string, object?>("tipo", item.Key));
        }
    }
}
