using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SbaCars.BuildingBlocks.Messaging.HealthChecks;

/// <summary>
/// Registers <see cref="RabbitMqReadinessHealthCheck"/> — called once, from each of the four
/// services' <c>Program.cs</c> (§8), next to <c>AddSbaCarsPostgresReadinessCheck</c>, mirroring
/// <c>PersistenceHealthChecksExtensions.AddSbaCarsPostgresReadinessCheck{TContext}</c>.
/// </summary>
public static class MessagingHealthChecksExtensions
{
    /// <summary>
    /// Adds the RabbitMQ readiness check named <c>"rabbitmq"</c>, tagged with <paramref name="tag"/> —
    /// in practice always <c>SbaCars.BuildingBlocks.Observability.HealthCheckTags.Ready</c>, kept as a
    /// plain parameter here (rather than a direct reference to that constant) so this project does
    /// not need to depend on <c>BuildingBlocks.Observability</c> just to read one tag name.
    /// </summary>
    public static IHealthChecksBuilder AddSbaCarsRabbitMqReadinessCheck(
        this IHealthChecksBuilder builder,
        string tag)
    {
        // TryAddSingleton, not AddSingleton: this method's only contract is "the check works when
        // added", not "AddSbaCarsMessaging must run first". The probe is cheap to register and does
        // not open its connection until the first check actually runs (see RabbitMqConnectionProbe),
        // so registering it here — regardless of whether AddSbaCarsMessaging already did — is safe
        // and keeps this extension usable on its own.
        builder.Services.TryAddSingleton<RabbitMqConnectionProbe>();

        return builder.AddCheck<RabbitMqReadinessHealthCheck>("rabbitmq", tags: [tag]);
    }
}
