using System.Diagnostics;

namespace SbaCars.BuildingBlocks.Observability.Sanitization;

/// <summary>
/// Redacts sensitive values from the two key/value shapes telemetry actually exports in:
/// <see cref="Activity"/> tags (traces) and structured log state (logs). Both surfaces only ever
/// see names and values — never the original DTO instance — so, unlike
/// <see cref="SensitiveDataSanitizer.Sanitize"/>, redaction here is by property <em>name</em>
/// (<see cref="SensitiveDataSanitizer.GetSensitivePropertyNames"/>), matched case-insensitively
/// against the DTO type whose properties were used to populate the tags/state in the first place.
/// </summary>
/// <remarks>
/// Deliberately built on plain <c>System.Diagnostics.Activity</c> (part of the BCL) rather than an
/// <c>OpenTelemetry.BaseProcessor&lt;Activity&gt;</c>: A8 configures OpenTelemetry, not this task
/// (§5.7 explicitly scopes A4b to the sanitization *mechanism*). A8's trace processor becomes a
/// one-line adapter — <c>public override void OnEnd(Activity a) =&gt; SensitiveTagRedactor.Redact(a,
/// typeof(SomeSensitiveDto));</c> — instead of this task pulling in the OpenTelemetry SDK to build
/// a pipeline nothing wires up yet.
/// </remarks>
public static class SensitiveTagRedactor
{
    /// <summary>
    /// Replaces every tag on <paramref name="activity"/> whose key matches one of
    /// <paramref name="payloadType"/>'s <see cref="SensitiveDataAttribute"/>-marked property names
    /// with <see cref="SensitiveDataSanitizer.RedactedValue"/>. Call this once, right before the
    /// activity ends (an OTel <c>BaseProcessor&lt;Activity&gt;.OnEnd</c>, or an
    /// <c>ActivityListener.ActivityStopped</c> callback) — tags added after redaction runs are not
    /// covered.
    /// </summary>
    public static void Redact(Activity activity, Type payloadType)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(payloadType);

        Redact(activity, SensitiveDataSanitizer.GetSensitivePropertyNames(payloadType));
    }

    /// <summary>
    /// Overload for when the sensitive names are already known (e.g. merged from more than one
    /// payload type contributing tags to the same activity), so the caller does not have to reflect
    /// over a type just to reuse this method.
    /// </summary>
    public static void Redact(Activity activity, IReadOnlySet<string> sensitiveNames)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(sensitiveNames);

        if (sensitiveNames.Count == 0)
        {
            return;
        }

        // Snapshot first: SetTag inside the enumeration of TagObjects would mutate the same
        // backing list being iterated.
        var tagNames = activity.TagObjects.Select(tag => tag.Key).ToArray();
        foreach (var tagName in tagNames)
        {
            if (sensitiveNames.Contains(tagName))
            {
                activity.SetTag(tagName, SensitiveDataSanitizer.RedactedValue);
            }
        }
    }

    /// <summary>
    /// Redacts a structured log state list (the shape <c>ILogger</c>'s templated logging exposes to
    /// a custom provider — each placeholder in the message template becomes one
    /// <c>KeyValuePair&lt;string, object?&gt;</c>) against <paramref name="payloadType"/>'s marked
    /// property names. Returns a new list; the input is never mutated.
    /// </summary>
    public static IReadOnlyList<KeyValuePair<string, object?>> RedactLogState(
        IReadOnlyList<KeyValuePair<string, object?>> state,
        Type payloadType)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(payloadType);

        var sensitiveNames = SensitiveDataSanitizer.GetSensitivePropertyNames(payloadType);
        if (sensitiveNames.Count == 0)
        {
            return state;
        }

        var redacted = new KeyValuePair<string, object?>[state.Count];
        for (var i = 0; i < state.Count; i++)
        {
            var (key, value) = state[i];
            redacted[i] = sensitiveNames.Contains(key)
                ? new KeyValuePair<string, object?>(key, SensitiveDataSanitizer.RedactedValue)
                : state[i];
        }

        return redacted;
    }
}
