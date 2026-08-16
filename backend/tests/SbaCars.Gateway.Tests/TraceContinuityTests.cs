extern alias GatewayPublic;

using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace SbaCars.Gateway.Tests;

/// <summary>
/// The A8 "Pronto quando" criterion, proved automatically rather than only by hand against the
/// compose stack: "Requisição do SPA aparece como um trace único atravessando gateway e serviço".
/// Injects a W3C <c>traceparent</c> at the edge — standing in for the browser's OpenTelemetry Web
/// SDK, which is what actually starts the trace in production — and proves the downstream service
/// participates in the *same* trace-id with the *correct* parent span: not merely "some span
/// shares the trace-id", but "the service's inbound span's parent is a span the gateway itself
/// produced". Reuses A7's <see cref="WebApplicationFactory{TEntryPoint}"/> + destination-stub
/// pattern from <see cref="PublicGatewayBehaviorTests"/>, with
/// <see cref="InstrumentedDestinationStub"/> standing in for catalog-service so the real
/// <c>AddSbaCarsObservability</c>/ASP.NET Core instrumentation mechanism — the same one every real
/// service and gateway calls identically — runs on both sides of the hop, without needing to boot
/// a full service host (Postgres, Logto) just to observe trace propagation, a concern that lives
/// entirely in <c>BuildingBlocks.Observability</c> and ASP.NET Core/YARP's own instrumentation.
/// </summary>
public sealed class TraceContinuityTests
{
    [Fact]
    public async Task ATraceInjectedAtTheEdge_ContinuesWithTheSameTraceId_AndTheCorrectParentSpan_IntoTheService()
    {
        await using var destination = await InstrumentedDestinationStub.StartAsync();

        var gatewayExportedActivities = new List<Activity>();
        await using var factory = new WebApplicationFactory<GatewayPublic::Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ReverseProxy:Clusters:catalog:Destinations:destination1:Address"] = destination.Address,
                }));
            builder.ConfigureServices(services =>
                services.ConfigureOpenTelemetryTracerProvider((_, tracing) =>
                    tracing.AddInMemoryExporter(gatewayExportedActivities)));
        });
        using var client = factory.CreateClient();

        var injectedTraceId = ActivityTraceId.CreateRandom();
        var injectedSpanId = ActivitySpanId.CreateRandom();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/catalog/_probe/ping");
        // The exact header shape the browser's W3C propagator attaches (§8): version-traceid-spanid-flags.
        request.Headers.Add("traceparent", $"00-{injectedTraceId.ToHexString()}-{injectedSpanId.ToHexString()}-01");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // The gateway's own root span for this request: the one whose parent is exactly the
        // injected span — i.e. gateway-public continued the trace the "browser" started instead
        // of beginning a new one of its own.
        var gatewayInboundSpan = gatewayExportedActivities.Should()
            .ContainSingle(activity => activity.TraceId == injectedTraceId && activity.ParentSpanId == injectedSpanId)
            .Subject;

        // ASP.NET Core's and HttpClient's ActivityListeners are process-wide (neither
        // "Microsoft.AspNetCore" nor "System.Net.Http" is scoped per TracerProvider), so the
        // stub's own exported list also picks up gateway-public's own inbound span and its
        // outbound Client-kind span for this same call — all real, all correctly parented, just
        // not the service's own inbound span this assertion is about. The rewritten path
        // (§2.3: /api/catalog/{**rest} → /api/{**rest}) is the one tag only the destination's own
        // inbound span carries — the gateway's inbound span has the pre-rewrite path instead.
        var serviceInboundSpan = destination.ExportedActivities.Should()
            .ContainSingle(activity => activity.Kind == ActivityKind.Server &&
                activity.GetTagItem("url.path") as string == "/api/_probe/ping")
            .Subject;

        // Same trace-id all the way through — the literal "trace único" of the A8 criterion.
        gatewayInboundSpan.TraceId.Should().Be(injectedTraceId);
        serviceInboundSpan.TraceId.Should().Be(injectedTraceId);

        // "Span pai correto": the service's inbound span's parent must be a span the gateway
        // itself produced (YARP's own proxy span or the HttpClient outbound span it wraps) — not
        // the browser's injected span id directly, and not a fresh, parentless root.
        serviceInboundSpan.ParentSpanId.Should().NotBe(injectedSpanId);
        gatewayExportedActivities.Should().Contain(activity => activity.SpanId == serviceInboundSpan.ParentSpanId,
            "the service's parent span must be one the gateway itself produced while proxying the request");
    }
}
