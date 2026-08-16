using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using SbaCars.Contracts;

namespace SbaCars.Architecture.Tests;

/// <summary>
/// Reflects public <see cref="IIntegrationEvent"/> <c>record</c> types and produces canonical JSON for
/// the B4 schema gate (§9): wire name from <see cref="IntegrationEventAttribute"/>, CLR full name, and
/// each property's name, canonical type name, and nullability. Lives in Architecture.Tests — not in
/// <c>SbaCars.Contracts</c> — so the vocabulary project stays dependency-free.
/// </summary>
internal static class ContractSchemaSnapshot
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private static readonly NullabilityInfoContext NullabilityContext = new();

    public static string BuildCanonicalJson(Assembly contractsAssembly) =>
        JsonSerializer.Serialize(CollectEventSchemas(contractsAssembly), JsonOptions);

    public static IReadOnlyList<IntegrationEventSchema> CollectEventSchemas(Assembly assembly) =>
        DiscoverIntegrationEventTypes(assembly)
            .Select(ToSchema)
            .OrderBy(schema => schema.WireName, StringComparer.Ordinal)
            .ToArray();

    public static bool SchemasAreEquivalent(
        IReadOnlyList<IntegrationEventSchema> committed,
        IReadOnlyList<IntegrationEventSchema> live,
        out string? difference)
    {
        var committedJson = JsonSerializer.Serialize(committed, JsonOptions);
        var liveJson = JsonSerializer.Serialize(live, JsonOptions);

        if (string.Equals(committedJson, liveJson, StringComparison.Ordinal))
        {
            difference = null;
            return true;
        }

        difference = BuildDifferenceSummary(committed, live);
        return false;
    }

    public static bool JsonDocumentsAreEquivalent(string committedJson, string liveJson, out string? difference)
    {
        var committed = Deserialize(committedJson);
        var live = Deserialize(liveJson);
        return SchemasAreEquivalent(committed, live, out difference);
    }

    public static IReadOnlyList<IntegrationEventSchema> Deserialize(string json) =>
        JsonSerializer.Deserialize<List<IntegrationEventSchema>>(json, JsonOptions)
            ?? throw new InvalidOperationException("Contract schema snapshot JSON deserialized to null.");

    private static IEnumerable<Type> DiscoverIntegrationEventTypes(Assembly assembly) =>
        assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .Where(type => typeof(IIntegrationEvent).IsAssignableFrom(type))
            .Where(type => type.GetCustomAttribute<IntegrationEventAttribute>() is not null);

    private static IntegrationEventSchema ToSchema(Type eventType)
    {
        var wireName = eventType.GetCustomAttribute<IntegrationEventAttribute>()!.Name;

        var properties = eventType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .Select(property => new IntegrationEventPropertySchema(
                property.Name,
                GetCanonicalTypeName(property.PropertyType),
                IsNullable(property)))
            .ToArray();

        return new IntegrationEventSchema(wireName, eventType.FullName!, properties);
    }

    private static string GetCanonicalTypeName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return GetCanonicalTypeName(underlying);
        }

        return type.FullName ?? type.Name;
    }

    private static bool IsNullable(PropertyInfo property)
    {
        if (Nullable.GetUnderlyingType(property.PropertyType) is not null)
        {
            return true;
        }

        if (property.PropertyType.IsValueType)
        {
            return false;
        }

        var nullability = NullabilityContext.Create(property);
        return nullability.ReadState == NullabilityState.Nullable;
    }

    private static string BuildDifferenceSummary(
        IReadOnlyList<IntegrationEventSchema> committed,
        IReadOnlyList<IntegrationEventSchema> live)
    {
        var committedByWire = committed.ToDictionary(schema => schema.WireName, StringComparer.Ordinal);
        var liveByWire = live.ToDictionary(schema => schema.WireName, StringComparer.Ordinal);

        var details = new List<string>();

        foreach (var wireName in committedByWire.Keys.Union(liveByWire.Keys).Order(StringComparer.Ordinal))
        {
            var hasCommitted = committedByWire.TryGetValue(wireName, out var committedSchema);
            var hasLive = liveByWire.TryGetValue(wireName, out var liveSchema);

            if (!hasCommitted)
            {
                details.Add($"added event '{wireName}'");
                continue;
            }

            if (!hasLive)
            {
                details.Add($"removed event '{wireName}'");
                continue;
            }

            if (!string.Equals(committedSchema!.ClrFullName, liveSchema!.ClrFullName, StringComparison.Ordinal))
            {
                details.Add(
                    $"event '{wireName}' CLR type changed from '{committedSchema.ClrFullName}' to '{liveSchema.ClrFullName}'");
            }

            CompareProperties(wireName, committedSchema.Properties, liveSchema.Properties, details);
        }

        return details.Count == 0
            ? "schemas differ but no per-event detail was produced — compare JSON directly"
            : string.Join("; ", details);
    }

    private static void CompareProperties(
        string wireName,
        IReadOnlyList<IntegrationEventPropertySchema> committed,
        IReadOnlyList<IntegrationEventPropertySchema> live,
        List<string> details)
    {
        var committedByName = committed.ToDictionary(property => property.Name, StringComparer.Ordinal);
        var liveByName = live.ToDictionary(property => property.Name, StringComparer.Ordinal);

        foreach (var propertyName in committedByName.Keys.Union(liveByName.Keys).Order(StringComparer.Ordinal))
        {
            var hasCommitted = committedByName.TryGetValue(propertyName, out var committedProperty);
            var hasLive = liveByName.TryGetValue(propertyName, out var liveProperty);

            if (!hasCommitted)
            {
                details.Add($"event '{wireName}' added property '{propertyName}'");
                continue;
            }

            if (!hasLive)
            {
                details.Add($"event '{wireName}' removed property '{propertyName}'");
                continue;
            }

            if (!string.Equals(committedProperty!.Type, liveProperty!.Type, StringComparison.Ordinal))
            {
                details.Add(
                    $"event '{wireName}' property '{propertyName}' type changed from '{committedProperty.Type}' to '{liveProperty.Type}'");
            }

            if (committedProperty.IsNullable != liveProperty.IsNullable)
            {
                details.Add(
                    $"event '{wireName}' property '{propertyName}' nullability changed from {committedProperty.IsNullable} to {liveProperty.IsNullable}");
            }
        }
    }
}

internal sealed record IntegrationEventSchema(
    string WireName,
    string ClrFullName,
    IReadOnlyList<IntegrationEventPropertySchema> Properties);

internal sealed record IntegrationEventPropertySchema(string Name, string Type, bool IsNullable);
