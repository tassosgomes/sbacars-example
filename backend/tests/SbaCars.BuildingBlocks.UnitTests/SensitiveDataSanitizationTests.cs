using System.Diagnostics;
using SbaCars.BuildingBlocks.Observability.Sanitization;

namespace SbaCars.BuildingBlocks.UnitTests;

/// <summary>
/// Proves the §5.7 sanitization mechanism: a property marked <see cref="SensitiveDataAttribute"/>
/// never survives <see cref="SensitiveDataSanitizer.Sanitize"/>, a trace tag populated from it, or a
/// structured log state entry — while an unmarked property passes through untouched. Modeled on the
/// shape D04's dossier will eventually have (RN-12): CPF and declared income are sensitive, an item
/// reference is not.
/// </summary>
public sealed class SensitiveDataSanitizationTests
{
    private sealed class DossiePayload
    {
        public Guid JornadaId { get; init; }

        [SensitiveData]
        public string Cpf { get; init; } = string.Empty;

        [SensitiveData]
        public decimal RendaDeclarada { get; init; }

        public string ItemDoCatalogoId { get; init; } = string.Empty;
    }

    private static DossiePayload CreateSamplePayload() => new()
    {
        JornadaId = Guid.CreateVersion7(),
        Cpf = "123.456.789-00",
        RendaDeclarada = 12_345.67m,
        ItemDoCatalogoId = "item-99",
    };

    [Fact]
    public void Sanitize_MasksPropertiesMarkedSensitive_AndLeavesOthersUntouched()
    {
        var payload = CreateSamplePayload();

        var sanitized = SensitiveDataSanitizer.Sanitize(payload);

        sanitized[nameof(DossiePayload.Cpf)].Should().Be(SensitiveDataSanitizer.RedactedValue);
        sanitized[nameof(DossiePayload.RendaDeclarada)].Should().Be(SensitiveDataSanitizer.RedactedValue);
        sanitized[nameof(DossiePayload.ItemDoCatalogoId)].Should().Be(payload.ItemDoCatalogoId);
        sanitized[nameof(DossiePayload.JornadaId)].Should().Be(payload.JornadaId);
    }

    [Fact]
    public void Sanitize_OnAPlainObjectWithNoSensitiveProperties_ReturnsAllValuesUnmasked()
    {
        var plain = new { ItemDoCatalogoId = "item-1", Preco = 10m };

        var sanitized = SensitiveDataSanitizer.Sanitize(plain);

        sanitized.Values.Should().NotContain(SensitiveDataSanitizer.RedactedValue);
    }

    [Fact]
    public void Sanitize_AppliedToAnIntegrationEventPayload_RedactsItBeforeItWouldReachTheOutbox()
    {
        // §5.7: "vale também para o payload dos eventos de integração ... persistido no outbox".
        // Outbox wiring is Phase B (B2); this proves the sanitization step a future
        // publisher/outbox writer would run the raw payload through.
        var eventPayload = CreateSamplePayload();

        var sanitizedForTransit = SensitiveDataSanitizer.Sanitize(eventPayload);

        sanitizedForTransit[nameof(DossiePayload.Cpf)].Should().Be(SensitiveDataSanitizer.RedactedValue);
        sanitizedForTransit.Should().ContainKey(nameof(DossiePayload.JornadaId), "the event's business key must survive sanitization");
    }

    [Fact]
    public void RedactActivity_MasksTagsPopulatedFromSensitiveProperties_BeforeTheSpanWouldExport()
    {
        using var activitySource = new ActivitySource(nameof(RedactActivity_MasksTagsPopulatedFromSensitiveProperties_BeforeTheSpanWouldExport));
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = activitySource.StartActivity("dossie.lido");
        activity.Should().NotBeNull();

        var payload = CreateSamplePayload();
        activity!.SetTag(nameof(DossiePayload.Cpf), payload.Cpf);
        activity.SetTag(nameof(DossiePayload.ItemDoCatalogoId), payload.ItemDoCatalogoId);

        SensitiveTagRedactor.Redact(activity, typeof(DossiePayload));

        activity.TagObjects.Should().ContainSingle(tag => tag.Key == nameof(DossiePayload.Cpf) &&
            Equals(tag.Value, SensitiveDataSanitizer.RedactedValue));
        activity.TagObjects.Should().ContainSingle(tag => tag.Key == nameof(DossiePayload.ItemDoCatalogoId) &&
            Equals(tag.Value, payload.ItemDoCatalogoId));
    }

    [Fact]
    public void RedactLogState_MasksEntriesWhoseKeyIsSensitive_LeavingOthersUntouched()
    {
        // The shape ILogger's templated logging exposes to a custom provider for
        // `logger.LogInformation("Dossie {Cpf} do item {ItemDoCatalogoId} lido", cpf, itemId)`.
        IReadOnlyList<KeyValuePair<string, object?>> logState =
        [
            new(nameof(DossiePayload.Cpf), "123.456.789-00"),
            new(nameof(DossiePayload.ItemDoCatalogoId), "item-99"),
        ];

        var redacted = SensitiveTagRedactor.RedactLogState(logState, typeof(DossiePayload));

        redacted.Should().ContainSingle(kvp => kvp.Key == nameof(DossiePayload.Cpf) &&
            Equals(kvp.Value, SensitiveDataSanitizer.RedactedValue));
        redacted.Should().ContainSingle(kvp => kvp.Key == nameof(DossiePayload.ItemDoCatalogoId) &&
            Equals(kvp.Value, "item-99"));
    }
}
