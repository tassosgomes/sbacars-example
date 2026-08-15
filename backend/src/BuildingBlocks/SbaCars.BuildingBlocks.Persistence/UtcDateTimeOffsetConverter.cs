using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SbaCars.BuildingBlocks.Persistence;

/// <summary>
/// Forces every <see cref="DateTimeOffset"/> that crosses the EF Core boundary to carry a UTC
/// offset, in both directions. A value with any other offset is converted, never rejected: the
/// instant it represents does not change, only how it is expressed.
/// </summary>
public sealed class UtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTimeOffset>
{
    public UtcDateTimeOffsetConverter()
        : base(value => value.ToUniversalTime(), value => value.ToUniversalTime())
    {
    }
}

/// <summary>
/// Nullable counterpart of <see cref="UtcDateTimeOffsetConverter"/>, for optional timestamp
/// columns.
/// </summary>
public sealed class NullableUtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset?, DateTimeOffset?>
{
    public NullableUtcDateTimeOffsetConverter()
        : base(
            value => value.HasValue ? value.Value.ToUniversalTime() : value,
            value => value.HasValue ? value.Value.ToUniversalTime() : value)
    {
    }
}
