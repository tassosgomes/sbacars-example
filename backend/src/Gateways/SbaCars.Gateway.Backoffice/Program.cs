using SbaCars.BuildingBlocks.Web.Auth;
using SbaCars.BuildingBlocks.Web.CorrelationId;
using SbaCars.BuildingBlocks.Web.Cors;
using SbaCars.BuildingBlocks.Web.ErrorHandling;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSbaCarsProblemDetails();
builder.Services.AddSbaCarsCors(builder.Configuration);
builder.Services.AddSbaCarsAuth(builder.Configuration, builder.Environment);

var app = builder.Build();

// IExceptionHandler must be first: it wraps everything downstream in a try/catch.
app.UseExceptionHandler();
app.UseSbaCarsCorrelationId();
app.UseHttpsRedirection();
app.UseSbaCarsCors();
// No rate limiting here: gateway-backoffice never accepts unauthenticated traffic (§2.3), so
// the anonymous-surface protection that matters lives only in gateway-public.

// Validates and rejects at the edge (§5.2). Route proxying to the four services is YARP's job
// (A7) — this only wires the pipeline so that whatever routes A7 adds inherit default deny.
app.UseSbaCarsAuth();

app.Run();
