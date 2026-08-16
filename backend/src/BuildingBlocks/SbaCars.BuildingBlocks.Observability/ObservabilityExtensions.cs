using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SbaCars.BuildingBlocks.Observability.Sanitization;

namespace SbaCars.BuildingBlocks.Observability;

/// <summary>
/// OpenTelemetry wiring (§8 of the architecture plan) — tracing, metrics and logs, all exported
/// via OTLP to the Aspire Dashboard (§10) — called identically by every one of the six processes
/// (the four <c>Api</c> and the two gateways). Nothing here is specific to a service or an edge:
/// what differs between processes is only the <paramref name="serviceName"/> passed in, and,
/// for the four services, the extra Npgsql tracing each wires from its own Infrastructure project
/// (see the remark on the containing <c>.csproj</c>) and the health checks each registers next to
/// this call — never this method's own behavior.
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Registers the OpenTelemetry SDK for tracing, metrics and logs — instrumentation is always
    /// on; OTLP export turns on only when <see cref="ObservabilitySettings.OtlpEndpoint"/> is
    /// configured (see the remarks below for why that split, rather than requiring the setting, is
    /// the deliberate choice here).
    /// </summary>
    /// <param name="sensitivePayloadTypes">
    /// DTO/event payload types whose <see cref="Sanitization.SensitiveDataAttribute"/>-marked
    /// property names must never survive onto an exported span tag (§5.7) — merged into the single
    /// <see cref="SensitiveDataRedactionProcessor"/> registered on the tracing pipeline. Defaults
    /// to none: no service has a sensitive DTO yet (that is D03/D04 work); the processor is wired
    /// now as mechanism, ready for the day a service passes its first type in.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>Why a missing OTLP endpoint does not fail the boot.</b> Every other configuration
    /// section in this codebase that is genuinely required for the process to do its job
    /// (<c>Cors</c>, <c>Jwt</c>, <c>Persistence</c>) fails <c>ValidateOnStart</c> when absent
    /// (§4.4) — missing configuration is a deploy mistake, and the process should never limp along
    /// half-configured. Telemetry is different: a developer running a single service with F5,
    /// with no Aspire Dashboard started, is not misconfigured — not exporting traces is a
    /// perfectly valid state for that inner loop, and refusing to boot over it would make the
    /// dashboard mandatory just to run the code. So <see cref="ObservabilitySettings.OtlpEndpoint"/>
    /// is optional: unset, every OTLP exporter below is simply never added, and the SDK's own
    /// instrumentation stays fully wired (ASP.NET Core, HttpClient, runtime meters, log scopes),
    /// which is what lets <c>SbaCars.Gateway.Tests</c> attach an in-memory exporter on top of an
    /// unconfigured pipeline and still observe real spans. When the setting <em>is</em> present but
    /// malformed, that is exactly the deploy-mistake case the other sections guard against, so
    /// <see cref="ObservabilitySettingsValidator"/> still fails the boot (§4.4) — and when it is
    /// present but the dashboard is simply unreachable (wrong host, dashboard not started yet in
    /// compose), that is a runtime/network condition the OTLP exporter's own background batch
    /// export already tolerates (it retries and drops on failure without touching request
    /// handling), so it never surfaces as a boot failure either.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddSbaCarsObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        IReadOnlyCollection<Type>? sensitivePayloadTypes = null)
    {
        services.AddOptions<ObservabilitySettings>()
            .Bind(configuration.GetSection(ObservabilitySettings.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ObservabilitySettings>, ObservabilitySettingsValidator>();

        // Read synchronously, ahead of the options pipeline above: whether to add an OTLP
        // exporter is a decision the TracerProviderBuilder/MeterProviderBuilder/logging pipeline
        // needs to make right now, while the host is still being configured — not something that
        // can wait for DI to resolve IOptions<T> later. A malformed value here is deliberately
        // swallowed into "no exporter" rather than thrown: ValidateOnStart above is what actually
        // fails the boot on it (§4.4), once the host starts — throwing here, at registration time,
        // would surface as a raw UriFormatException instead of the OptionsValidationException
        // every other misconfigured section in this codebase fails with.
        var settings = configuration.GetSection(ObservabilitySettings.SectionName).Get<ObservabilitySettings>()
            ?? new ObservabilitySettings();
        var otlpEndpoint = settings.OtlpEndpoint is { Length: > 0 } endpoint &&
            Uri.TryCreate(endpoint, UriKind.Absolute, out var parsedEndpoint)
                ? parsedEndpoint
                : null;

        var serviceVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0";
        var serviceInstanceId = Guid.NewGuid().ToString();

        var redactionProcessor = new SensitiveDataRedactionProcessor(sensitivePayloadTypes ?? []);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: serviceName,
                serviceVersion: serviceVersion,
                serviceInstanceId: serviceInstanceId))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(serviceName)
                    // YARP's own span for the proxying operation itself (harmless no-op on the
                    // four services, which never create an activity under this name) — without
                    // it, only the ASP.NET Core inbound span and the HttpClient outbound span
                    // would show up on a gateway, hiding YARP's own overhead between the two.
                    .AddSource("Yarp.ReverseProxy")
                    .AddAspNetCoreInstrumentation(o =>
                        // Health polling is infrastructure noise, not a business request — keeps
                        // traces (and the dashboard) focused on requests that matter.
                        o.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                    .AddHttpClientInstrumentation()
                    // §5.7: the one-line adapter SensitiveTagRedactor's <remarks> describes,
                    // registered here so it runs on every span this process ends, whether or not
                    // any payload type is marked sensitive today.
                    .AddProcessor(redactionProcessor);

                if (otlpEndpoint is not null)
                {
                    tracing.AddOtlpExporter(otlp => otlp.Endpoint = otlpEndpoint);
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(serviceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (otlpEndpoint is not null)
                {
                    metrics.AddOtlpExporter(otlp => otlp.Endpoint = otlpEndpoint);
                }
            });

        // AddLogging (rather than builder.Logging.AddOpenTelemetry) keeps this method an
        // IServiceCollection extension like every other AddSbaCarsX in the codebase — WebApplicationBuilder.Logging
        // is an ILoggingBuilder the host exposes separately, but AddLogging(Action<ILoggingBuilder>)
        // reaches the same configuration surface additively, without needing this method to take
        // the whole builder just for this one signal.
        services.AddLogging(logging => logging.AddOpenTelemetry(otel =>
        {
            // The correlation id already lives in the ILogger scope CorrelationIdMiddleware opens
            // (A3) — IncludeScopes is the one flag that makes it (and TraceId/SpanId, which the
            // SDK attaches from Activity.Current automatically) show up on the exported LogRecord.
            // No second correlation mechanism is introduced here.
            otel.IncludeScopes = true;
            otel.ParseStateValues = true;
            otel.IncludeFormattedMessage = true;
            otel.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(
                serviceName: serviceName,
                serviceVersion: serviceVersion,
                serviceInstanceId: serviceInstanceId));

            if (otlpEndpoint is not null)
            {
                otel.AddOtlpExporter(otlp => otlp.Endpoint = otlpEndpoint);
            }
        }));

        return services;
    }
}
