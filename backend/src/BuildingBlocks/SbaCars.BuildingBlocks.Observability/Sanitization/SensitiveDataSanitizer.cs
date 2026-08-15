using System.Collections.Concurrent;
using System.Reflection;

namespace SbaCars.BuildingBlocks.Observability.Sanitization;

/// <summary>
/// Redacts properties marked <see cref="SensitiveDataAttribute"/> before an object reaches a
/// structured log line, an OTLP export or an integration event payload (§5.7 of the foundation
/// plan). This is the one mechanism every surface in this namespace builds on:
/// <see cref="Sanitize"/> for a DTO/event payload as a whole, <see cref="GetSensitivePropertyNames"/>
/// for surfaces that only have key/value pairs to work with (trace tags, log state) — see
/// <see cref="SensitiveTagRedactor"/>.
/// </summary>
/// <remarks>
/// Masks rather than removes the affected keys (the plan allows either — "remove ou mascara").
/// Masking keeps the payload's shape stable: a log parser or a dashboard built against
/// "this event always has a <c>Cpf</c> key" does not silently break the day the value is redacted,
/// and a redacted-but-present key is easier to notice in a log line than an omitted one, which
/// simply looks like the field was never populated.
/// </remarks>
public static class SensitiveDataSanitizer
{
    public const string RedactedValue = "***REDACTED***";

    private static readonly ConcurrentDictionary<Type, IReadOnlyList<PropertyInfo>> SensitivePropertiesByType = new();

    /// <summary>
    /// Returns a snapshot of <paramref name="value"/>'s public instance properties, with every
    /// property carrying <see cref="SensitiveDataAttribute"/> replaced by
    /// <see cref="RedactedValue"/>. Safe to serialize (to JSON for an event payload, or to a log
    /// line) without leaking the original values.
    /// </summary>
    public static IReadOnlyDictionary<string, object?> Sanitize(object value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var type = value.GetType();
        var properties = GetSanitizableProperties(type);
        var sensitive = GetSensitiveProperties(type);

        var result = new Dictionary<string, object?>(properties.Count);
        foreach (var property in properties)
        {
            result[property.Name] = sensitive.Contains(property)
                ? RedactedValue
                : property.GetValue(value);
        }

        return result;
    }

    /// <summary>
    /// The names of <paramref name="type"/>'s properties marked <see cref="SensitiveDataAttribute"/>,
    /// case-insensitively comparable — for surfaces that redact by key name rather than by holding
    /// an instance (<see cref="SensitiveTagRedactor"/>, structured log state).
    /// </summary>
    public static IReadOnlySet<string> GetSensitivePropertyNames(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return GetSensitiveProperties(type)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<PropertyInfo> GetSanitizableProperties(Type type) =>
        SensitivePropertiesByType.GetOrAdd(type, static t => t
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .ToArray());

    private static HashSet<PropertyInfo> GetSensitiveProperties(Type type) =>
        GetSanitizableProperties(type)
            .Where(property => property.IsDefined(typeof(SensitiveDataAttribute), inherit: false))
            .ToHashSet();
}
