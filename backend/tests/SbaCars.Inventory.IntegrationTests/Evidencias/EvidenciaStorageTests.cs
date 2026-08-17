using System.Net;
using System.Net.Http.Headers;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SbaCars.BuildingBlocks.Application;
using SbaCars.BuildingBlocks.Storage;
using SbaCars.Inventory.Application.Common;
using SbaCars.Inventory.Application.Evidencias.GerarUrlDownload;
using SbaCars.Inventory.Application.Evidencias.GerarUrlUpload;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Infrastructure.Storage;
using SbaCars.TestKit;

using InventoryMinioCollection = SbaCars.Inventory.IntegrationTests.InventoryMinioCollection;

namespace SbaCars.Inventory.IntegrationTests.Evidencias;

[Collection(InventoryMinioCollection.Name)]
public sealed class EvidenciaStorageTests
{
    private readonly SbaCarsMinioFixture _fixture;

    public EvidenciaStorageTests(SbaCarsMinioFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PresignedUploadThenDownload_ReturnsTheSameBytes()
    {
        var bucket = UniqueBucketName("evidencia-upload");
        var ofertaId = Guid.CreateVersion7();
        var payload = "sbacars-evidencia-proof"u8.ToArray();
        const string contentType = "application/pdf";
        const string fileName = "laudo.pdf";

        using var provider = BuildProvider(bucket);
        var storage = provider.GetRequiredService<IObjectStorage>();
        var s3 = provider.GetRequiredService<IAmazonS3>();
        await s3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });

        var now = DateTimeOffset.UtcNow;
        var evidenciaRepository = new InMemoryEvidenciaRepository();
        var uploadHandler = new GerarUrlUploadHandler(
            new StubOfertaRepository(ofertaId),
            evidenciaRepository,
            storage,
            new FixedStorageSettings(bucket),
            new NoOpUnitOfWork(),
            new StubCurrentUser("operator-1", "Ana"),
            new FixedClock(now));

        var uploadResponse = await uploadHandler.HandleAsync(
            new GerarUrlUploadCommand
            {
                OfertaId = ofertaId,
                NomeArquivo = fileName,
                TipoConteudo = contentType,
                TamanhoBytes = payload.Length,
            },
            CancellationToken.None);

        using var http = new HttpClient();
        using var putContent = new ByteArrayContent(payload);
        putContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        foreach (var (headerName, headerValue) in uploadResponse.HeadersObrigatorios)
        {
            putContent.Headers.Remove(headerName);
            putContent.Headers.TryAddWithoutValidation(headerName, headerValue);
        }

        var putResponse = await http.PutAsync(uploadResponse.UploadUrl, putContent);
        putResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        var downloadHandler = new GerarUrlDownloadHandler(
            evidenciaRepository,
            storage,
            new FixedStorageSettings(bucket));

        var downloadResponse = await downloadHandler.HandleAsync(
            new GerarUrlDownloadQuery(uploadResponse.EvidenciaId),
            CancellationToken.None);

        var downloaded = await http.GetByteArrayAsync(downloadResponse.DownloadUrl);
        downloaded.Should().Equal(payload);
        downloadResponse.NomeArquivo.Should().Be(fileName);
    }

    [Fact]
    public async Task AnonymousGetWithoutSignature_IsDenied()
    {
        var bucket = UniqueBucketName("evidencia-anonymous");
        var key = $"ofertas/{Guid.CreateVersion7():N}/evidencias/{Guid.CreateVersion7():N}/laudo.pdf";
        var payload = "private-evidencia"u8.ToArray();
        const string contentType = "application/pdf";

        using var provider = BuildProvider(bucket);
        var storage = provider.GetRequiredService<IObjectStorage>();
        var s3 = provider.GetRequiredService<IAmazonS3>();
        await s3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });

        var upload = await storage.CreateUploadUrlAsync(bucket, key, contentType);
        using var http = new HttpClient();
        using var putContent = new ByteArrayContent(payload);
        putContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        (await http.PutAsync(upload.Url, putContent)).IsSuccessStatusCode.Should().BeTrue();

        var anonymousUrl = new Uri(new Uri(_fixture.ServiceUrl), $"{bucket}/{key}");
        var anonymousResponse = await http.GetAsync(anonymousUrl);

        anonymousResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    private static string UniqueBucketName(string prefix) =>
        $"{prefix}-{Guid.NewGuid():N}".ToLowerInvariant();

    private ServiceProvider BuildProvider(string bucketName)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:ServiceUrl"] = _fixture.ServiceUrl,
                ["Storage:AccessKey"] = _fixture.AccessKey,
                ["Storage:SecretKey"] = _fixture.SecretKey,
                ["Storage:ForcePathStyle"] = "true",
                ["Storage:Region"] = "us-east-1",
                ["Storage:BucketName"] = bucketName,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSbaCarsStorage(configuration);
        return services.BuildServiceProvider();
    }

    private sealed class StubOfertaRepository(Guid ofertaId) : IOfertaRepository
    {
        public Task<Oferta?> ObterAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Oferta?>(
                id == ofertaId
                    ? Oferta.Criar(
                        new Veiculo(TipoVeiculo.CarroUsado, placa: "ABC1D23"),
                        new Autoria("operator-1", "Ana", DateTimeOffset.UtcNow),
                        DateTimeOffset.UtcNow)
                    : null);

        public Task<bool> ExistePlacaAtivaAsync(
            string placa,
            Guid? ignorarOfertaId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public void Adicionar(Oferta oferta) => throw new NotSupportedException();

        public void Remover(Oferta oferta) => throw new NotSupportedException();
    }

    private sealed class InMemoryEvidenciaRepository : IEvidenciaRepository
    {
        private readonly List<Evidencia> _items = [];

        public Task<Evidencia?> ObterAsync(Guid evidenciaId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.SingleOrDefault(item => item.Id == evidenciaId));

        public Task<IReadOnlyList<Evidencia>> ObterVariosAsync(
            IEnumerable<Guid> evidenciaIds,
            CancellationToken cancellationToken = default)
        {
            var ids = evidenciaIds.ToHashSet();
            return Task.FromResult<IReadOnlyList<Evidencia>>(
                _items.Where(item => ids.Contains(item.Id)).ToArray());
        }

        public void Adicionar(Evidencia item) => _items.Add(item);
    }

    private sealed class FixedStorageSettings(string bucketName) : IInventoryStorageSettings
    {
        public string BucketName => bucketName;
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class StubCurrentUser(string userId, string displayName) : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public string? UserId => userId;

        public string? DisplayName => displayName;

        public IReadOnlyCollection<string> Permissions => ["estoque:gerenciar"];

        public bool HasPermission(string permission) => Permissions.Contains(permission);
    }
}
