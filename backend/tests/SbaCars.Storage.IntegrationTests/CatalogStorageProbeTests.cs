using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SbaCars.BuildingBlocks.Observability;
using SbaCars.BuildingBlocks.Storage;
using SbaCars.BuildingBlocks.Storage.HealthChecks;
using SbaCars.BuildingBlocks.Web.Auth;
using SbaCars.Catalog.Api.Controllers;
using SbaCars.TestKit;

namespace SbaCars.Storage.IntegrationTests;

/// <summary>
/// C3 proof: catalog storage probe endpoints enforce <see cref="Permissoes.CatalogoGerenciar"/>,
/// presigned upload/download round-trip bytes, anonymous bucket access is denied, and readiness
/// reports S3 healthy — all against real MinIO without booting the full Catalog.Api host.
/// </summary>
[Collection(SbaCarsMinioCollection.Name)]
public sealed class CatalogStorageProbeTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SbaCarsMinioFixture _fixture;

    public CatalogStorageProbeTests(SbaCarsMinioFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task NoToken_UploadUrl_Returns401()
    {
        var (app, _, _) = await StartHostAsync();
        await using var _ = app;
        using var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync(
            "/api/_probe/storage/upload-url",
            new { contentType = "application/octet-stream" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenWithoutCatalogoGerenciar_UploadUrl_Returns403()
    {
        var (app, signingKey, _) = await StartHostAsync();
        await using var _ = app;
        using var client = CreateAuthorizedClient(app, signingKey, scope: "estoque:ler");

        var response = await client.PostAsJsonAsync(
            "/api/_probe/storage/upload-url",
            new { contentType = "application/octet-stream" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TokenWithCatalogoGerenciar_UploadThenDownload_RoundTripsBytes()
    {
        var (app, signingKey, _) = await StartHostAsync();
        await using var _ = app;
        using var client = CreateAuthorizedClient(app, signingKey, scope: "catalogo:gerenciar");
        var payload = "sbacars-c3-catalog-probe"u8.ToArray();
        const string contentType = "application/octet-stream";

        var uploadResponse = await client.PostAsJsonAsync(
            "/api/_probe/storage/upload-url",
            new { contentType });
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var upload = await uploadResponse.Content.ReadFromJsonAsync<StoragePresignedUrlResponse>(JsonOptions);
        upload.Should().NotBeNull();
        upload!.Bucket.Should().Be(ObjectStorageBuckets.CatalogMedia);
        upload.Key.Should().StartWith("probes/");

        using var http = new HttpClient();
        using var putContent = new ByteArrayContent(payload);
        putContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        foreach (var (headerName, headerValue) in upload.RequiredHeaders)
        {
            putContent.Headers.Remove(headerName);
            putContent.Headers.TryAddWithoutValidation(headerName, headerValue);
        }

        var putResult = await http.PutAsync(upload.Url, putContent);
        putResult.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        var downloadResponse = await client.PostAsJsonAsync(
            "/api/_probe/storage/download-url",
            new { key = upload.Key });
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var download = await downloadResponse.Content.ReadFromJsonAsync<StoragePresignedUrlResponse>(JsonOptions);
        download.Should().NotBeNull();

        var downloaded = await http.GetByteArrayAsync(download!.Url);
        downloaded.Should().Equal(payload);
    }

    [Fact]
    public async Task AnonymousGetWithoutSignature_IsDenied()
    {
        var (app, signingKey, bucket) = await StartHostAsync();
        await using var _ = app;
        using var client = CreateAuthorizedClient(app, signingKey, scope: "catalogo:gerenciar");
        var payload = "private-probe-object"u8.ToArray();
        const string contentType = "text/plain";

        var uploadResponse = await client.PostAsJsonAsync(
            "/api/_probe/storage/upload-url",
            new { contentType });
        var upload = await uploadResponse.Content.ReadFromJsonAsync<StoragePresignedUrlResponse>(JsonOptions);
        upload.Should().NotBeNull();

        using var http = new HttpClient();
        using var putContent = new ByteArrayContent(payload);
        putContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        (await http.PutAsync(upload!.Url, putContent)).IsSuccessStatusCode.Should().BeTrue();

        var anonymousUrl = new Uri(new Uri(_fixture.ServiceUrl), $"{bucket}/{upload.Key}");
        var anonymousResponse = await http.GetAsync(anonymousUrl);

        anonymousResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReadyHealthCheck_IncludesHealthyS3()
    {
        var (app, _, _) = await StartHostAsync();
        await using var _ = app;
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var report = await response.Content.ReadFromJsonAsync<HealthReportJson>(JsonOptions);
        report.Should().NotBeNull();
        report!.Checks.Should().Contain(check => check.Name == "s3" && check.Status == "Healthy");
    }

    private async Task<(WebApplication App, RsaSecurityKey SigningKey, string Bucket)> StartHostAsync()
    {
        var bucket = ObjectStorageBuckets.CatalogMedia;
        var signingKey = TestJwt.CreateSigningKey();

        var prepServices = new ServiceCollection();
        prepServices.AddSbaCarsStorage(StorageTestConfiguration.Build(_fixture, bucket));
        await using var prep = prepServices.BuildServiceProvider();
        var s3 = prep.GetRequiredService<IAmazonS3>();
        await StorageTestConfiguration.EnsureBucketExistsAsync(s3, bucket);

        var app = await TestHostFactory.StartAsync(
            configureBuilder: builder =>
            {
                builder.Services
                    .AddControllers()
                    .AddApplicationPart(typeof(ProbeController).Assembly);
                builder.Services.AddSbaCarsAuth(builder.Configuration, builder.Environment);
                builder.Services.PostConfigure<JwtBearerOptions>(
                    JwtBearerDefaults.AuthenticationScheme,
                    options =>
                    {
                        options.Authority = null;
                        options.RequireHttpsMetadata = false;
                        options.TokenValidationParameters.ValidIssuer = TestJwt.Issuer;
                        options.TokenValidationParameters.IssuerSigningKey = signingKey;
                    });
                builder.Services.AddSbaCarsStorage(StorageTestConfiguration.Build(_fixture, bucket));
                builder.Services.AddSbaCarsHealthChecks()
                    .AddSbaCarsS3ReadinessCheck(HealthCheckTags.Ready);
            },
            configureApp: webApp =>
            {
                webApp.UseSbaCarsAuth();
                webApp.MapControllers();
                webApp.MapSbaCarsHealthChecks();
            });

        return (app, signingKey, bucket);
    }

    private static HttpClient CreateAuthorizedClient(
        WebApplication app,
        RsaSecurityKey signingKey,
        string scope)
    {
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.Create(signingKey, scope: scope));
        return client;
    }

    private sealed record StoragePresignedUrlResponse(
        string Bucket,
        string Key,
        Uri Url,
        Dictionary<string, string> RequiredHeaders,
        DateTimeOffset ExpiresAt);

    private sealed record HealthReportJson(string Status, HealthCheckEntryJson[] Checks);

    private sealed record HealthCheckEntryJson(string Name, string Status);
}
