using SbaCars.BuildingBlocks.Observability;
using SbaCars.BuildingBlocks.Web.Auth;
using SbaCars.BuildingBlocks.Web.CorrelationId;
using SbaCars.BuildingBlocks.Web.Cors;
using SbaCars.BuildingBlocks.Web.ErrorHandling;
using SbaCars.BuildingBlocks.Web.Runtime;
using SbaCars.Gateway.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSbaCarsProblemDetails();
builder.Services.AddSbaCarsCors(builder.Configuration);
builder.Services.AddSbaCarsAuth(builder.Configuration, builder.Environment);
// Routes for all four services (§2.3). Every route carries its own explicit AuthorizationPolicy
// ("Default" — YARP's built-in name for "require an authenticated user") in appsettings.json, on
// top of, not instead of, the default-deny FallbackPolicy AddSbaCarsAuth already registered
// above. gateway-backoffice rejects the unauthenticated request at the edge; the service it
// would have reached revalidates independently regardless (§5.2) — deliberate redundancy, not
// removed here. Every cluster there sets HttpRequest.ActivityTimeout explicitly (30s, same
// rationale as gateway-public) — required by §8, not left to YARP's own default.
builder.Services.AddSbaCarsReverseProxy(builder.Configuration);
builder.Services.AddSbaCarsObservability(builder.Configuration, "gateway-backoffice");
builder.Services.AddSbaCarsRuntimeReadiness(builder.Configuration);
// gateway-backoffice is the one gateway that also calls AddSbaCarsAuth (§5.2), so it gets the
// JWKS leg of readiness on top of the shared self + downstream-services checks (§8).
builder.Services.AddSbaCarsGatewayHealthChecks()
    .AddSbaCarsJwksReadinessCheck(HealthCheckTags.Ready);
// RabbitMQ, S3/MinIO and Redis do not exist yet (Fases B, C and D6) — nothing to check for them.

var app = builder.Build();

// IExceptionHandler must be first: it wraps everything downstream in a try/catch.
app.UseExceptionHandler();
app.UseSbaCarsCorrelationId();
app.UseHttpsRedirection();
app.UseSbaCarsRequestTimeouts();
app.UseSbaCarsCors();
// No rate limiting here: gateway-backoffice never accepts unauthenticated traffic (§2.3), so
// the anonymous-surface protection that matters lives only in gateway-public.

// Validates and rejects at the edge (§5.2). Must run before UseSbaCarsReverseProxy so every
// proxied route is covered by authentication/authorization, including its own AuthorizationPolicy.
app.UseSbaCarsAuth();
app.UseSbaCarsReverseProxy();
// Anonymous on purpose (§8): the default-deny FallbackPolicy above would otherwise block an
// orchestrator's health probe, which never carries a token.
app.MapSbaCarsHealthChecks();

app.Run();
