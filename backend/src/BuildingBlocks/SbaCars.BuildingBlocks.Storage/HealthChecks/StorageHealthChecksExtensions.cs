using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SbaCars.BuildingBlocks.Storage.HealthChecks;

/// <summary>
/// Registers <see cref="S3ReadinessHealthCheck"/> — called from each service's <c>Program.cs</c>
/// in C3, mirroring <c>MessagingHealthChecksExtensions.AddSbaCarsRabbitMqReadinessCheck</c>.
/// </summary>
public static class StorageHealthChecksExtensions
{
    /// <summary>
    /// Adds the S3 readiness check named <c>"s3"</c>, tagged with <paramref name="tag"/>.
    /// </summary>
    public static IHealthChecksBuilder AddSbaCarsS3ReadinessCheck(
        this IHealthChecksBuilder builder,
        string tag)
    {
        return builder.AddCheck<S3ReadinessHealthCheck>("s3", tags: [tag]);
    }
}
