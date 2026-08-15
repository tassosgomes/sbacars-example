using SbaCars.BuildingBlocks.Web.CorrelationId;
using SbaCars.BuildingBlocks.Web.ErrorHandling;
using SbaCars.BuildingBlocks.Web.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSbaCarsProblemDetails();
builder.Services.AddSbaCarsOpenApi();

var app = builder.Build();

// IExceptionHandler must be first: it wraps everything downstream in a try/catch.
app.UseExceptionHandler();
app.UseSbaCarsCorrelationId();
app.UseHttpsRedirection();
app.UseSbaCarsOpenApi();

app.UseAuthorization();

app.MapControllers();

app.Run();
