using SbaCars.BuildingBlocks.Web.CorrelationId;
using SbaCars.BuildingBlocks.Web.Cors;
using SbaCars.BuildingBlocks.Web.ErrorHandling;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSbaCarsProblemDetails();
builder.Services.AddSbaCarsCors(builder.Configuration);

var app = builder.Build();

// IExceptionHandler must be first: it wraps everything downstream in a try/catch.
app.UseExceptionHandler();
app.UseSbaCarsCorrelationId();
app.UseHttpsRedirection();
app.UseSbaCarsCors();
// No rate limiting here: gateway-backoffice never accepts unauthenticated traffic (§2.3), so
// the anonymous-surface protection that matters lives only in gateway-public.

app.Run();
