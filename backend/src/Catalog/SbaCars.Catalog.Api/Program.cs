using SbaCars.BuildingBlocks.Web.Auth;
using SbaCars.BuildingBlocks.Web.CorrelationId;
using SbaCars.BuildingBlocks.Web.ErrorHandling;
using SbaCars.BuildingBlocks.Web.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSbaCarsProblemDetails();
builder.Services.AddSbaCarsOpenApi();
builder.Services.AddSbaCarsAuth(builder.Configuration, builder.Environment);

var app = builder.Build();

// IExceptionHandler must be first: it wraps everything downstream in a try/catch.
app.UseExceptionHandler();
app.UseSbaCarsCorrelationId();
app.UseHttpsRedirection();
app.UseSbaCarsOpenApi();

// Revalidates the token independently of gateway-backoffice (§5.2): a service never trusts the
// edge for its own authorization. Default deny — every endpoint requires authentication unless
// explicitly marked [AllowAnonymous].
app.UseSbaCarsAuth();

app.MapControllers();

app.Run();
