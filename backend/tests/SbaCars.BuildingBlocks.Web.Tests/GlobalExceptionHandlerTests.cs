using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using SbaCars.BuildingBlocks.Domain;
using SbaCars.BuildingBlocks.Web.ErrorHandling;

namespace SbaCars.BuildingBlocks.Web.Tests;

public sealed class GlobalExceptionHandlerTests
{
    private const string SecretConnectionDetail = "Server=prod-db;User Id=sa;Password=Sup3rSecret!";

    [Fact]
    public async Task UnhandledException_ReturnsGenericProblemDetailsWithTraceIdAndNoInternalDetail()
    {
        await using var app = await TestHostFactory.StartAsync(
            configureBuilder: builder => builder.Services.AddSbaCarsProblemDetails(),
            configureApp: app =>
            {
                app.UseExceptionHandler();
                Func<IResult> handler = () => throw new InvalidOperationException(SecretConnectionDetail);
                app.MapGet("/boom", handler);
            });

        using var client = app.GetTestClient();
        var response = await client.GetAsync("/boom");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var rawBody = await response.Content.ReadAsStringAsync();

        // Never leak the exception's own message, its type name, or a stack trace.
        rawBody.Should().NotContain(SecretConnectionDetail);
        rawBody.Should().NotContain(nameof(InvalidOperationException));
        rawBody.Should().NotContain("   at ");

        using var document = JsonDocument.Parse(rawBody);
        var root = document.RootElement;

        root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status500InternalServerError);
        root.TryGetProperty("traceId", out var traceId).Should().BeTrue();
        traceId.GetString().Should().NotBeNullOrWhiteSpace();
    }

    private sealed class OutOfStockException(string message) : DomainException(message)
    {
    }

    [Fact]
    public async Task DomainException_ReturnsMappedStatusWithClientSafeMessageAndTraceId()
    {
        const string businessMessage = "Vehicle is no longer available.";

        await using var app = await TestHostFactory.StartAsync(
            configureBuilder: builder => builder.Services.AddSbaCarsProblemDetails(),
            configureApp: app =>
            {
                app.UseExceptionHandler();
                Func<IResult> handler = () => throw new OutOfStockException(businessMessage);
                app.MapGet("/business-error", handler);
            });

        using var client = app.GetTestClient();
        var response = await client.GetAsync("/business-error");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var rawBody = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(rawBody);
        var root = document.RootElement;

        root.GetProperty("detail").GetString().Should().Be(businessMessage);
        root.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    private sealed class VehicleNotFoundException(string message) : DomainException(message)
    {
    }

    [Fact]
    public async Task ServiceRegisteredException_UsesTheStatusItRegisteredWithoutTouchingBuildingBlocks()
    {
        await using var app = await TestHostFactory.StartAsync(
            configureBuilder: builder => builder.Services.AddSbaCarsProblemDetails(exceptions =>
                exceptions.Map<VehicleNotFoundException>(StatusCodes.Status404NotFound, "Not found")),
            configureApp: app =>
            {
                app.UseExceptionHandler();
                Func<IResult> handler = () => throw new VehicleNotFoundException("Vehicle abc123 was not found.");
                app.MapGet("/not-found", handler);
            });

        using var client = app.GetTestClient();
        var response = await client.GetAsync("/not-found");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
