using SbaCars.BuildingBlocks.Web.CorrelationId;
using SbaCars.BuildingBlocks.Web.Cors;
using SbaCars.BuildingBlocks.Web.ErrorHandling;
using SbaCars.BuildingBlocks.Web.RateLimiting;
using SbaCars.Gateway.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSbaCarsProblemDetails();
builder.Services.AddSbaCarsCors(builder.Configuration);
// Only gateway-public wires rate limiting: it is the one process that accepts anonymous
// traffic from the internet (§5.5 of the architecture plan).
builder.Services.AddSbaCarsRateLimiting(builder.Configuration);
// Anonymous surface only: catalog (read) and interest (write manifestation). Never inventory or
// purchase — that boundary lives in this gateway's own appsettings.json, on purpose, so an
// administrative route cannot leak onto the public edge by accident (§2.3 of the architecture
// plan). No AuthorizationPolicy on either route: gateway-public never calls AddSbaCarsAuth.
// Every cluster there sets HttpRequest.ActivityTimeout explicitly (30s — generous for a cold
// read, short enough that a stuck destination fails the request instead of holding the gateway's
// connection pool open) — required by §8, not left to YARP's own default.
builder.Services.AddSbaCarsReverseProxy(builder.Configuration);

var app = builder.Build();

// IExceptionHandler must be first: it wraps everything downstream in a try/catch.
app.UseExceptionHandler();
app.UseSbaCarsCorrelationId();
app.UseHttpsRedirection();
app.UseSbaCarsCors();
app.UseSbaCarsRateLimiting();
// Must come after UseSbaCarsRateLimiting: the interest write route opts into the stricter
// anonymous policy via RateLimiterPolicy in appsettings.json, which only takes effect if the
// rate limiter middleware already ran.
app.UseSbaCarsReverseProxy();

app.Run();
