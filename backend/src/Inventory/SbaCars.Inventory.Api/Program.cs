using SbaCars.BuildingBlocks.Web.Auditing;
using SbaCars.BuildingBlocks.Web.Auth;
using SbaCars.BuildingBlocks.Web.CorrelationId;
using SbaCars.BuildingBlocks.Web.ErrorHandling;
using SbaCars.BuildingBlocks.Web.OpenApi;
using SbaCars.Inventory.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSbaCarsProblemDetails();
builder.Services.AddSbaCarsOpenApi();
builder.Services.AddSbaCarsAuth(builder.Configuration, builder.Environment);
builder.Services.AddInventoryInfrastructure(builder.Configuration);

var app = builder.Build();

// IExceptionHandler must be first: it wraps everything downstream in a try/catch.
app.UseExceptionHandler();
app.UseSbaCarsCorrelationId();
app.UseHttpsRedirection();
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

app.Run();
