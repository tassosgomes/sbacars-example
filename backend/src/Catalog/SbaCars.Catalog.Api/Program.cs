using Rebus.Config;
using SbaCars.BuildingBlocks.Messaging;
using SbaCars.BuildingBlocks.Messaging.HealthChecks;
using SbaCars.BuildingBlocks.Observability;
using SbaCars.BuildingBlocks.Persistence.HealthChecks;
using SbaCars.BuildingBlocks.Web.Auditing;
using SbaCars.BuildingBlocks.Web.Auth;
using SbaCars.BuildingBlocks.Web.CorrelationId;
using SbaCars.BuildingBlocks.Web.ErrorHandling;
using SbaCars.BuildingBlocks.Web.OpenApi;
using SbaCars.BuildingBlocks.Web.Runtime;
using SbaCars.Catalog.Api.Messaging.Foundation;
using SbaCars.Catalog.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSbaCarsProblemDetails();
builder.Services.AddSbaCarsOpenApi();
builder.Services.AddSbaCarsAuth(builder.Configuration, builder.Environment);
builder.Services.AddCatalogInfrastructure(builder.Configuration);
builder.Services.AddSbaCarsObservability(builder.Configuration, "catalog-service");
builder.Services.AddSbaCarsMessaging(builder.Configuration, "catalog-service", CatalogDbContext.Schema);
// B5 scaffolding (§6.5) — delete with FoundationPingHandler when the first real catalog consumer exists.
builder.Services.AddSingleton<FoundationPingReceipt>();
builder.Services.AddRebusHandler<FoundationPingHandler>();
builder.Services.AddHostedService<FoundationPingSubscriptionHostedService>();
builder.Services.AddSbaCarsForwardedHeaders();
builder.Services.AddSbaCarsRuntimeReadiness(builder.Configuration);
builder.Services.AddSbaCarsHealthChecks()
    .AddSbaCarsPostgresReadinessCheck<CatalogDbContext>(HealthCheckTags.Ready)
    .AddSbaCarsJwksReadinessCheck(HealthCheckTags.Ready)
    .AddSbaCarsRabbitMqReadinessCheck(HealthCheckTags.Ready);
// S3/MinIO and Redis do not exist yet (Fases C and D6) — no health check for them here; adding
// one that always fails would make /health/ready lie about what this service actually needs
// today. RabbitMQ arrived in B1, above.

var app = builder.Build();

// Forwarded headers run before everything else: this service sits behind a gateway (§8,
// "Runtime" row), and every downstream concern that reads scheme or remote IP must see the real
// client, not the gateway's.
app.UseSbaCarsForwardedHeaders();

// IExceptionHandler must be first among the request-handling middleware: it wraps everything
// downstream in a try/catch.
app.UseExceptionHandler();
app.UseSbaCarsCorrelationId();
app.UseHttpsRedirection();
app.UseSbaCarsRequestTimeouts();
app.UseSbaCarsOpenApi();

// Flushes any sensitive-data reads buffered by this request's DbContext regardless of whether
// the request ever called SaveChanges, and regardless of whether it ends in a response or an
// exception (§5.7) — must stay nested inside UseExceptionHandler, above.
app.UseSbaCarsSensitiveDataAuditFlush();

// Revalidates the token independently of gateway-backoffice (§5.2): a service never trusts the
// edge for its own authorization. Default deny — every endpoint requires authentication unless
// explicitly marked [AllowAnonymous].
app.UseSbaCarsAuth();

app.MapControllers();
// Anonymous and outside the default-deny fallback policy on purpose (§8) — mapped after
// UseSbaCarsAuth only because Map* calls are order-independent relative to middleware; the
// [AllowAnonymous] MapSbaCarsHealthChecks applies is what actually exempts them.
app.MapSbaCarsHealthChecks();

app.Run();
