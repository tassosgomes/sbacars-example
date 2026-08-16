namespace SbaCars.BuildingBlocks.Observability;

/// <summary>
/// The <c>Observability</c> configuration section (§8 and §10 of the architecture plan).
/// <see cref="OtlpEndpoint"/> is the only knob: everything else about the pipeline (which
/// instrumentations run, resource attributes, log scopes) is identical in every environment and
/// lives in code, in <see cref="ObservabilityExtensions"/>.
/// </summary>
public sealed class ObservabilitySettings
{
    public const string SectionName = "Observability";

    /// <summary>
    /// The OTLP collector endpoint — the Aspire Dashboard in every environment this repository
    /// covers (§10). <see langword="null"/>/absent is a deliberate, supported value: it disables
    /// every OTLP exporter (tracing, metrics, logs) while leaving instrumentation registered, so a
    /// process boots cleanly in Development even when nobody has started the dashboard yet — see
    /// the remarks on <see cref="ObservabilityExtensions.AddSbaCarsObservability"/> for why that is
    /// the chosen failure mode instead of failing the boot. When set, it must be an absolute URI
    /// (validated by <see cref="ObservabilitySettingsValidator"/>) — a malformed value still fails
    /// fast at boot (§4.4), because that is a configuration typo, not "the dashboard is down".
    /// </summary>
    public string? OtlpEndpoint { get; set; }
}
