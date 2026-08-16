using System.Diagnostics;
using OpenTelemetry;

namespace SbaCars.BuildingBlocks.Observability.Sanitization;

/// <summary>
/// The <c>BaseProcessor&lt;Activity&gt;</c> adapter <see cref="SensitiveTagRedactor"/>'s
/// <c>&lt;remarks&gt;</c> describes: registered once on the tracing pipeline by
/// <see cref="ObservabilityExtensions.AddSbaCarsObservability"/>, it redacts every span this
/// process ends before the exporter ever sees it — the last point in-process where a tag
/// populated from a <see cref="SensitiveDataAttribute"/>-marked property could still leak (§5.7).
/// </summary>
/// <remarks>
/// Configurable by the set of sensitive property names, merged once at construction from however
/// many payload types the caller passes — a span can carry tags from more than one DTO (e.g. a
/// request DTO and a response DTO on the same activity), so redaction has to cover the union, not
/// just the first type that comes to mind.
/// </remarks>
public sealed class SensitiveDataRedactionProcessor : BaseProcessor<Activity>
{
    private readonly IReadOnlySet<string> _sensitiveNames;

    public SensitiveDataRedactionProcessor(IReadOnlyCollection<Type> sensitivePayloadTypes)
    {
        ArgumentNullException.ThrowIfNull(sensitivePayloadTypes);

        _sensitiveNames = sensitivePayloadTypes
            .SelectMany(SensitiveDataSanitizer.GetSensitivePropertyNames)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override void OnEnd(Activity data) => SensitiveTagRedactor.Redact(data, _sensitiveNames);
}
