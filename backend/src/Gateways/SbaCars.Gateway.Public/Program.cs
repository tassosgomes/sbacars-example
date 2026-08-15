using SbaCars.BuildingBlocks.Web.CorrelationId;
using SbaCars.BuildingBlocks.Web.Cors;
using SbaCars.BuildingBlocks.Web.ErrorHandling;
using SbaCars.BuildingBlocks.Web.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSbaCarsProblemDetails();
builder.Services.AddSbaCarsCors(builder.Configuration);
// Only gateway-public wires rate limiting: it is the one process that accepts anonymous
// traffic from the internet (§5.5 of the architecture plan).
builder.Services.AddSbaCarsRateLimiting(builder.Configuration);

var app = builder.Build();

// IExceptionHandler must be first: it wraps everything downstream in a try/catch.
app.UseExceptionHandler();
app.UseSbaCarsCorrelationId();
app.UseHttpsRedirection();
app.UseSbaCarsCors();
app.UseSbaCarsRateLimiting();

app.Run();
