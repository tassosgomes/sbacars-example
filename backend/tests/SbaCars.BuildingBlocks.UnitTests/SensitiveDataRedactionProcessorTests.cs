using System.Diagnostics;
using SbaCars.BuildingBlocks.Observability.Sanitization;

namespace SbaCars.BuildingBlocks.UnitTests;

/// <summary>
/// Proves the A8 leg of §5.7: <see cref="SensitiveDataRedactionProcessor"/> is the
/// <c>BaseProcessor&lt;Activity&gt;</c> adapter <see cref="SensitiveTagRedactor"/>'s
/// <c>&lt;remarks&gt;</c> promised — a tag populated from a marked property must not survive
/// <c>OnEnd</c>, the point where the OTel SDK hands the activity to whatever exporter is
/// configured next (§8 wires it into the tracing pipeline in <c>ObservabilityExtensions</c>).
/// </summary>
public sealed class SensitiveDataRedactionProcessorTests
{
    private sealed class DossiePayload
    {
        [SensitiveData]
        public string Cpf { get; init; } = string.Empty;

        public string ItemDoCatalogoId { get; init; } = string.Empty;
    }

    private static Activity StartTestActivity(out ActivitySource source, out ActivityListener listener)
    {
        source = new ActivitySource(nameof(SensitiveDataRedactionProcessorTests));
        listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        var activity = source.StartActivity("dossie.lido");
        activity.Should().NotBeNull();
        return activity!;
    }

    [Fact]
    public void OnEnd_RedactsTagsPopulatedFromSensitiveProperties_AndLeavesOthersUntouched()
    {
        using var activity = StartTestActivity(out var source, out var listener);
        using var __ = source;
        using var ___ = listener;

        activity.SetTag(nameof(DossiePayload.Cpf), "123.456.789-00");
        activity.SetTag(nameof(DossiePayload.ItemDoCatalogoId), "item-99");

        using var processor = new SensitiveDataRedactionProcessor([typeof(DossiePayload)]);
        processor.OnEnd(activity);

        activity.TagObjects.Should().ContainSingle(tag => tag.Key == nameof(DossiePayload.Cpf) &&
            Equals(tag.Value, SensitiveDataSanitizer.RedactedValue));
        activity.TagObjects.Should().ContainSingle(tag => tag.Key == nameof(DossiePayload.ItemDoCatalogoId) &&
            Equals(tag.Value, "item-99"));
    }

    [Fact]
    public void OnEnd_WithNoSensitivePayloadTypesConfigured_LeavesEveryTagUntouched()
    {
        // The default AddSbaCarsObservability wires today, since no service has a sensitive DTO
        // yet — the processor must be a safe no-op, not a silent redaction of everything.
        using var activity = StartTestActivity(out var source, out var listener);
        using var __ = source;
        using var ___ = listener;

        activity.SetTag(nameof(DossiePayload.Cpf), "123.456.789-00");

        using var processor = new SensitiveDataRedactionProcessor([]);
        processor.OnEnd(activity);

        activity.TagObjects.Should().ContainSingle(tag => tag.Key == nameof(DossiePayload.Cpf) &&
            Equals(tag.Value, "123.456.789-00"));
    }
}
